// using ECommerceAPI.Data;
// using EcommerceAPI.DTOs;
// using ECommerceAPI.Models;
// using Microsoft.AspNetCore.Mvc;
// using Microsoft.EntityFrameworkCore;
//
// namespace EcommerceAPI.Controllers
// {
//     [Route("api/[controller]")]
//     [ApiController]
//     public class CustomersController : ControllerBase
//     {
//         private readonly ECommerceDbContext _context;
//
//         public CustomersController(ECommerceDbContext context)
//         {
//             _context = context;
//         }
//
//         // Register a new customer.
//         // Demonstrates [FromForm].
//         // Endpoint: POST /api/customers/register
//         [HttpPost("register")]
//         public async Task<ActionResult<Customer>> RegisterCustomer([FromBody] CustomerRegistrationDTO registrationDto) // Binding from form data
//         {
//             // Check if email already exists
//             if (await _context.Customers.AnyAsync(e => e.Email == registrationDto.Email))
//             {
//                 return BadRequest("Email already exists.");
//             }
//
//             var customer = new Customer
//             {
//                 Name = registrationDto.Name,
//                 Email = registrationDto.Email,
//                 Password = registrationDto.Password,
//                 City = registrationDto.City
//             };
//
//             _context.Customers.Add(customer);
//             await _context.SaveChangesAsync();
//
//             return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, customer);
//         }
//
//         // Login a customer.
//         // Demonstrates [FromHeader] and [FromBody].
//         // Endpoint: POST /api/customers/login
//         [HttpPost("login")]
//         public async Task<IActionResult> Login(
//             [FromHeader(Name = "X-Client-ID")] string clientId, // Binding from header
//             [FromBody] CustomerLoginDTO loginDto) // Binding from body
//         {
//             // Check the custom header
//             if (string.IsNullOrWhiteSpace(clientId))
//                 return BadRequest("Missing X-Client-ID header");
//
//             var customer = await _context.Customers
//                 .FirstOrDefaultAsync(c => c.Email == loginDto.Email && c.Password == loginDto.Password);
//
//             if (customer == null)
//             {
//                 return Unauthorized("Invalid email or password.");
//             }
//
//             // Generate JWT or other token in real applications
//             return Ok(new { Message = "Authentication successful." });
//         }
//
//         // Get customer details.
//         // Demonstrates default binding (from route or query).
//         // Endpoint: GET /api/customers/{id}
//         [HttpGet("{id:int}")]
//         public async Task<ActionResult<Customer>> GetCustomer([FromRoute] int id) // Default binding from route
//         {
//             var customer = await _context.Customers.FindAsync(id);
//
//             if (customer == null)
//             {
//                 return NotFound();
//             }
//
//             return Ok(customer);
//         }
//     }
// }