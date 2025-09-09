using AutoMapper;
using EcommerceAPI.Cache;
using ECommerceAPI.Data;
using EcommerceAPI.DTOs;
using EcommerceAPI.Middleware;
using ECommerceAPI.Models;
using EcommerceAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
// using Microsoft.Extensions.Caching.Memory;

namespace EcommerceAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CustomersController : ControllerBase
    {
        private readonly ECommerceDbContext _context;
        private readonly IMapper _mapper;
        private readonly CustomerCache _cache;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;

        public CustomersController(ECommerceDbContext context,
            IMapper mapper, CustomerCache cache, 
            TokenService tokenService, IConfiguration config
            )
        {
            _context = context;
            _mapper = mapper;
            _cache = cache;
            _tokenService = tokenService;
            _config = config;
        }
        
        // Register a new customer.
        // Demonstrates [FromForm].
        // Endpoint: POST /api/customers/register
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<ActionResult<CustomerRegistrationDTO>> RegisterCustomer([FromBody] CustomerRegistrationDTO registrationDto) // Binding from form data
        {
            // Check if email already exists
            if (await _context.Customers.AnyAsync(e => e.Email == registrationDto.Email))
            {
                return BadRequest("Email already exists.");
                
            }
            PasswordCrypt.CreatePasswordHash(registrationDto.Password, out string passwordHash, out string passwordSalt);
            try
            {
                var customer = new Customer
                {
                    FirstName = registrationDto.FirstName,
                    LastName = registrationDto.LastName,
                    Email = registrationDto.Email,
                    Password = passwordHash,
                    PhoneNumber = registrationDto.PhoneNumber,
                    DateOfBirth = registrationDto.DateOfBirth,
                    IsActive = registrationDto.IsActive,
                    MembershipTierId = registrationDto.MembershipTierId
                };

                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
                var token = _tokenService.CreateToken(customer.Id.ToString(), customer.Email);
                var response = new
                {
                    Token = token,
                    Expiring = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"),
                    registeredCustomer = _mapper.Map<Customer>(customer)
                };
                
                return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // Login a customer.
        // Demonstrates [FromHeader] and [FromBody].
        // Endpoint: POST /api/customers/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(
            [FromHeader(Name = "X-Client-ID")] string? clientId, // Binding from header
            [FromBody] CustomerLoginDTO loginDto) // Binding from body
        {
            // Check the custom header
            if (string.IsNullOrWhiteSpace(clientId))
                return BadRequest("Missing X-Client-ID header");

            var customer = await _context.Customers
                .FirstOrDefaultAsync(c => c.Email == loginDto.Email);
            if (!PasswordCrypt.VerifyPasswordHash(loginDto.Password, customer.Password))
                return Unauthorized("Invalid email or password.");
            
                    // && c.Password == loginDto.Password

            if (customer == null!)
            {
                return Unauthorized("Invalid email or password.");
            }
            var token = _tokenService.CreateToken(customer.Id.ToString(), customer.Email);
            
            
            // Generate JWT or other token in real applications
            return Ok(new
            {
                Token = token,
                ExpiresInMinutes = int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60"),
                HttpResponseMessage = "Authentication Successful"
            });
            // return Ok(new { Message = "Authentication successful." });
        }

        // Get customer details.
        // Demonstrates default binding (from route or query).
        // Endpoint: GET /api/customers/{id}
        [HttpGet("{id:int}")]
        public async Task<ActionResult<Customer>> GetCustomer([FromRoute] int id) // Default binding from route
        {
            var customer = await _cache.GetCustomers(id);

            // if (customer == null)
            // {
            //     return NotFound();
            // }

            return Ok(customer);
        }
    }
}