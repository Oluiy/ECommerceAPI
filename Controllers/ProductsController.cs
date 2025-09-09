using AutoMapper;
using EcommerceAPI.Cache;
using ECommerceAPI.Data;
using EcommerceAPI.DTOs;
using ECommerceAPI.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace EcommerceAPI.CustomersController
{
    [Route("api/[controller]")]
    [ApiController] 
    public class ProductsController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMapper _mapper;
        private readonly ProductCache _cache;
        
        public ProductsController(ECommerceDbContext context, IMapper mapper, ProductCache cache)
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
        }

        // Get all products with optional filtering by name, category, and price range.
        // Demonstrates [FromQuery] and default binding.
        // Endpoint: GET /api/products/GetProducts?name={name}&category={category}&minPrice={minPrice}&maxPrice={maxPrice}
        [HttpGet("GetProducts")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts(
            [FromQuery] string? name, // Explicitly binding from query string
            [FromQuery] int? stockQuantity, // Explicitly binding from query string
            [FromQuery] decimal? minPrice, // Explicitly binding from query string
            decimal? maxPrice) // Default binding; since it's a simple type, binds from query string
        {
            var query = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(name))
            {
                query = query.Where(p => p.Name.Contains(name));
                // query = query.Select(u => u.Name).Where(u => u.Contains(name)).OrderBy(string.Equals("e")).ToListAsync();
            }

            if (stockQuantity.HasValue)
            {
                query = query.Where(p =>p.StockQuantity == stockQuantity.Value);
            }

            if (minPrice.HasValue)
            {
                query = query.Where(p => p.Price >= minPrice.Value);
            }

            if (maxPrice.HasValue)
            {
                query = query.Where(p => p.Price <= maxPrice.Value);
            }

            var products = await query.ToListAsync();
            return Ok(products);
        }

        // Get a specific product by ID.
        // Demonstrates [FromRoute] and default binding.
        // Endpoint: GET /api/products/GetProductById/{id}
        [HttpGet("GetProductById/{id:int}")]
        public async Task<ActionResult<Product>> GetProductById([FromRoute] int id) // Explicitly binding from route
        {
            var product = await _cache.GetProdById(id);
            // var product = await _context.Products.FindAsync(id);
            

            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        // Create a new product.
        // Demonstrates [FromBody] binding.
        // Endpoint: POST /api/products/CreateProduct
        [HttpPost("CreateProduct")]
        public async Task<ActionResult<Product>> CreateProduct([FromBody] ProductCreateDTO productCreateDto) 
        {
            // Mapping from ProductCreateDto to Product entity
            var product = new Product
            {
                Name = productCreateDto.Name,
                Description = productCreateDto.Description,
                Price = productCreateDto.Price,
                DiscountPercentage = productCreateDto.DiscountPercentage,
                SKU = productCreateDto.SKU,
                StockQuantity = productCreateDto.StockQuantity,
                CategoryId = productCreateDto.CategoryId
            };

            _context.Products.Add(product);
            await _context.SaveChangesAsync();

            // Returns 201 Created with the location header
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        // Update an existing product's price.
        // Demonstrates multiple binding attributes.
        // Endpoint: PUT /api/products/UpdateProductPrice/{id}?price={newPrice}
        [HttpPut("UpdateProductPrice/{id}")]
        public async Task<IActionResult> UpdateProductPrice(
            [FromRoute] int id, // Binding from route
            [FromQuery] decimal price) // Binding from query string
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null)
            {
                return NotFound();
            }

            product.Price = price;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // Demonstrates [FromQuery] for pagination
        // Endpoint: GET: /api/products/paged?pageNumber={pageNumber}&pageSize={pageSize}
        [HttpGet("paged")]
        public async Task<ActionResult<List<Product>>> GetProductsPaged([FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 5)
        {
            var products = await _context.Products
                                   .Skip((pageNumber - 1) * pageSize)
                                   .Take(pageSize)
                                   .AsNoTracking()
                                   .ToListAsync();

            return Ok(products);
        }

        // Upload product image.
        // Demonstrates [FromForm]
        // Endpoint: POST /api/products/{id}/upload
        [HttpPost("{id}/upload")]
        public async Task<IActionResult> UploadProductImage(
            [FromRoute] int id, // Binding from route
             IFormFile file) // Binding from form data
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var product = await _context.Products.FindAsync(id);
            if (product == null)
                return NotFound();

            // For demonstration, we'll just read the file name.
            // In a real application, you'd save the file to storage and update the product's image URL.
            
            var fileName = Path.GetFileName(file.FileName);
            // TODO: Save the file and update product.ImageUrl

            return Ok(new { Message = "Image uploaded successfully.", FileName = fileName });
        }
    }
}