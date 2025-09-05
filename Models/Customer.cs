using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerceAPI.Models;

namespace ECommerceAPI.Models
{
    public class Customer
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Customer FirstName is Required."), MaxLength(100)]
        public string FirstName { get; set; }
        
        [Required(ErrorMessage = "Customer LastName is Required."), MaxLength(100)]
        public string LastName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid Email")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Password is required")]
        [StringLength(100)]
        public string Password { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime DateOfBirth { get; set; }
        public bool IsActive { get; set; }
        public int MembershipTierId { get; set; } // Foreign key to MembershipTier
        [ForeignKey("MembershipTierId")]
        public MembershipTier MembershipTier { get; set; }
        public List<Address> Addresses { get; set; }
        public List<Order> Orders { get; set; }
    }
}
