using System.ComponentModel.DataAnnotations;
using ECommerceAPI.Models;

namespace EcommerceAPI.DTOs
{
    // Data Transfer Object for customer registration.
    public class CustomerRegistrationDTO
    {
        [Required(ErrorMessage = "Customer Firstname is required")]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Customer Lastname is required")]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100)]
        public string Password { get; set; }
        [Required(ErrorMessage = "PhoneNumber is required!")]
        [Length(11, 11, ErrorMessage = "It must be 11 digits.")]
        public string PhoneNumber { get; set; }
        
        public DateTime DateOfBirth { get; set; }
        
        public bool IsActive { get; set; }
        
        public int MembershipTierId { get; set; }
    }
}