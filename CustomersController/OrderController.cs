using AutoMapper;
using AutoMapper.QueryableExtensions;   // Needed for .ProjectTo<T>()
using ECommerceAPI.Data;
using ECommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ECommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;

        public OrdersController(ECommerceDbContext context, IMapper mapper, IConfiguration configuration)
        {
            _context = context;
            _mapper = mapper;
            _configuration = configuration;
        }

        // Get an order by ID.
        // Demonstrates [FromRoute] 
        // Endpoint: GET /api/orders/GetOrderById/{id}
        [HttpGet("GetOrderById/{id}")]
        public async Task<ActionResult<OrderDTO>> GetOrderById([FromRoute] int id)
        {
            try
            {
                var orderDTO = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == id)
                    .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();
                if (orderDTO == null) return NotFound($"Order with ID {id} not found.");
                // If found, return a 200 OK along with the mapped OrderDTO
                return Ok(orderDTO);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred while fetching the order: {ex.Message}");
            }
        }

        [HttpGet("GetOrdersByCustomerId/{customerId}")]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetOrdersByCustomerId(int customerId)
        {
            try
            {
                // We filter by CustomerId, then project to OrderDTO and fetch the results
                var ordersDTO = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.CustomerId == customerId)
                    .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                    .ToListAsync();
                // If there are no matching orders, return 404
                if (!ordersDTO.Any())
                    return NotFound($"No orders found for customer with ID {customerId}.");
                // Otherwise, return them with a 200 OK
                return Ok(ordersDTO);
            }
            catch (Exception ex)
            {
                // A catch-all for any runtime issues
                return StatusCode(500,
                    $"An error occurred while fetching orders for customer {customerId}: {ex.Message}");
            }
        }

        // Create a new order.
        // Demonstrates [FromBody]
        // Endpoint: POST /api/orders/CreateOrder
        [HttpPost("CreateOrder")]
        public async Task<ActionResult<OrderDTO>> CreateOrder([FromBody] OrderCreateDTO orderCreateDTO)
        {
                // Validate incoming data quickly
            if (orderCreateDTO == null)
                return BadRequest("Order data cannot be null.");
                // Start a transaction to ensure either all operations succeed or none do
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Fetch the customer (and related membership + addresses) needed for discount & address validation
                //    We use AsNoTracking here because we don't want to update the customer's data 
                var customer = await _context.Customers
                    .AsNoTracking()
                    .Include(c => c.MembershipTier)
                    .Include(c => c.Addresses)
                    .FirstOrDefaultAsync(c => c.Id == orderCreateDTO.CustomerId && c.IsActive);
                // If the customer is invalid or not active, we stop here
                if (customer == null)
                    return NotFound($"Customer with ID {orderCreateDTO.CustomerId} not found or inactive.");
                // 2. Verify the shipping address belongs to this customer
                var shippingAddress = customer.Addresses
                    .FirstOrDefault(a => a.Id == orderCreateDTO.ShippingAddressId);
                // If no matching address, we return a BadRequest
                if (shippingAddress == null)
                    return BadRequest("The specified shipping address is invalid or does not belong to the customer.");
                // 3. Extract the product IDs from the incoming DTO
                var productIds = orderCreateDTO.Items.Select(i => i.ProductId).ToList();
                // 4. Performance optimization: only fetch columns we actually need 
                //    (Price, DiscountPercentage, StockQuantity).
                var productsData = await _context.Products
                    .Where(p => productIds.Contains(p.Id))
                    .Select(p => new
                    {
                        p.Id,
                        p.Price,
                        p.DiscountPercentage,
                        p.StockQuantity
                    })
                    .ToListAsync();
                // If the requested productIDs don't match what's in the DB, some are invalid
                if (productsData.Count != productIds.Count)
                    return BadRequest("One or more products in the order are invalid.");
                // 5. Ensure we have enough stock for each requested product
                foreach (var item in orderCreateDTO.Items)
                {
                    var productInfo = productsData.FirstOrDefault(p => p.Id == item.ProductId);
                    if (productInfo == null || productInfo.StockQuantity < item.Quantity)
                    {
                        return BadRequest($"Insufficient stock for product ID {item.ProductId}. " +
                                          $"Available: {productInfo?.StockQuantity ?? 0}");
                    }
                }

                // 6. Convert the OrderCreateDTO into an Order entity 
                //    (this sets basic fields like Status = "Pending" and Orderdate to DateTime.Now by default).
                var order = _mapper.Map<Order>(orderCreateDTO);
                // 7. Calculate the total amount of this order
                decimal totalAmount = 0;
                // We will need to update each order item's pricing details, and also 
                // reduce the product's stock in the database.
                foreach (var item in order.OrderItems)
                {
                    var productInfo = productsData.First(p => p.Id == item.ProductId);
                // Product-level discount
                    decimal productDiscount = productInfo.DiscountPercentage.HasValue
                        ? productInfo.Price * productInfo.DiscountPercentage.Value / 100
                        : 0;
                // Fill out the item details (price, discount, etc.)
                    item.ProductPrice = productInfo.Price;
                    item.Discount = productDiscount * item.Quantity;
                    item.TotalPrice = (productInfo.Price - productDiscount) * item.Quantity;
                // Accumulate into totalAmount which will be sored in the order level
                    totalAmount += item.TotalPrice;
                // Reduce stock in the actual tracked Product entity
                // We do a separate fetch by ID so we can update the real entity
                    var trackedProduct = await _context.Products.FindAsync(item.ProductId);
                    if (trackedProduct == null)
                        return BadRequest($"Product with ID {item.ProductId} no longer exists.");
                    trackedProduct.StockQuantity -= item.Quantity;
                }

                // 8. Calculate membership discount for the entire order
                decimal membershipDiscountPercentage = customer.MembershipTier?.DiscountPercentage ?? 0m;
                decimal orderDiscount = totalAmount * (membershipDiscountPercentage / 100);
                // 9. Determine delivery charge based on config settings and totalAmount
                bool applyDeliveryCharge = _configuration.GetValue<bool>("DeliveryChargeSettings:ApplyDeliveryCharge");
                decimal deliveryChargeAmount =
                    _configuration.GetValue<decimal>("DeliveryChargeSettings:DeliveryChargeAmount");
                decimal freeDeliveryThreshold =
                    _configuration.GetValue<decimal>("DeliveryChargeSettings:FreeDeliveryThreshold");
                decimal deliveryCharge = 0;
                if (applyDeliveryCharge && totalAmount < freeDeliveryThreshold)
                {
                    deliveryCharge = deliveryChargeAmount;
                }

                // 10. Final calculation for Amount, OrderDiscount, DeliveryCharge, and TotalAmount for the Product Level
                order.Amount = totalAmount;
                order.OrderDiscount = orderDiscount;
                order.DeliveryCharge = deliveryCharge;
                order.TotalAmount = totalAmount - orderDiscount + deliveryCharge;
                // 11. Add the new order to the DB context for insertion
                _context.Orders.Add(order);
                // Save changes: includes new order and updated stock
                await _context.SaveChangesAsync();
                // Commit the transaction so everything is permanent
                await transaction.CommitAsync();
                // 12. Retrieve the newly created order in a read-only manner using AsNoTracking,
                //     then ProjectTo<OrderDTO> to create the final response object.
                var createdOrderDTO = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == order.Id)
                    .ProjectTo<OrderDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();
                // If we can't find the newly-created order for some reason, throw 500
                if (createdOrderDTO == null)
                    return StatusCode(500, "An error occurred while creating the order.");
                // 13. Return a 200 OK response with the final OrderDTO
                return Ok(createdOrderDTO);
            }
            catch (Exception ex)
            {
                // Roll back the transaction if anything goes wrong
                await transaction.RollbackAsync();
                return StatusCode(500, $"An error occurred while creating the order: {ex.Message}");
            }

        }

        [HttpPost("AddTrackingDetails")]
        public async Task<ActionResult<TrackingDetailDTO>> AddTrackingDetails(
            [FromBody] TrackingCreateDTO trackingDetailDTO)
        {
                // Validate the input DTO
            if (trackingDetailDTO == null)
                return BadRequest("Invalid Tracking Details");
            try
            {
                // 1. Retrieve the order (tracked), including any existing TrackingDetail.
                var order = await _context.Orders
                    .Include(o => o.TrackingDetail)
                    .FirstOrDefaultAsync(o => o.Id == trackingDetailDTO.OrderId);
                // 2. If the order doesn't exist, return 404
                if (order == null)
                    return NotFound($"Order with ID {trackingDetailDTO.OrderId} not found.");
                // 3. Only allow updates if status is "Pending", "Processing", or "Shipped"
                //    - "Pending"/"Processing" transitions to "Shipped" upon adding tracking
                //    - If the order is already "Shipped", we allow further updates
                //    - Otherwise, return a 400 error.
                if (order.Status == "Pending" || order.Status == "Processing")
                {
                    order.Status = "Shipped";
                }
                else if (order.Status != "Shipped")
                {
                    return BadRequest($"Cannot add or update tracking for an order in status '{order.Status}'.");
                }

                // 4. If the order has no tracking detail, create one; otherwise, update it
                if (order.TrackingDetail == null)
                {
                    var trackingDetail = new TrackingDetail
                    {
                        OrderId = order.Id,
                        Carrier = trackingDetailDTO.Carrier,
                        EstimatedDeliveryDate = trackingDetailDTO.EstimatedDeliveryDate,
                        TrackingNumber = trackingDetailDTO.TrackingNumber
                    };
                    _context.TrackingDetails.Add(trackingDetail);
                }
                else
                {
                    order.TrackingDetail.Carrier = trackingDetailDTO.Carrier;
                    order.TrackingDetail.EstimatedDeliveryDate = trackingDetailDTO.EstimatedDeliveryDate;
                    order.TrackingDetail.TrackingNumber = trackingDetailDTO.TrackingNumber;
                }

                // 5. Save changes to the database
                await _context.SaveChangesAsync();
                // 6. Map the newly created or updated TrackingDetail to a TrackingDetailDTO
                //    Now that the database update is done, 'order.TrackingDetail' holds the latest data.
                var updatedTrackingDTO = _mapper.Map<TrackingDetailDTO>(order.TrackingDetail);
                // 7. Return 200 OK with the updated tracking info
                return Ok(updatedTrackingDTO);
            }
            catch (Exception ex)
            {
                // If anything goes wrong, return 500 with an error message
                return StatusCode(500, $"An error occurred while updating tracking details: {ex.Message}");
            }
        }
        
        [HttpGet("GetTrackingDetailByOrderId/{orderId}")]
        public async Task<ActionResult<TrackingDetailDTO>> GetTrackingDetailByOrderId(int orderId)
        {
            try
            {
                // Again, we only want the tracking details, so we filter to orders that
                // have a matching orderId AND a non-null TrackingDetail. Then we project 
                // that detail to TrackingDetailDTO.
                var trackingDetailDTO = await _context.Orders
                    .AsNoTracking()
                    .Where(o => o.Id == orderId && o.TrackingDetail != null)
                    .Select(o => o.TrackingDetail)
                    .ProjectTo<TrackingDetailDTO>(_mapper.ConfigurationProvider)
                    .FirstOrDefaultAsync();
                // If not found, return 404
                if (trackingDetailDTO == null)
                    return NotFound($"No tracking details found for order with ID {orderId}.");
                // Otherwise, return the mapped DTO
                return Ok(trackingDetailDTO);
            }
            catch (Exception ex)
            {
                // If something goes awry, respond with 500
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }
    }
}