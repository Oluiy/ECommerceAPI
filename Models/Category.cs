using System.ComponentModel.DataAnnotations;
using ECommerceAPI.Models;

namespace ECommerceAPI.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; } // Primary Key
        [Required, MaxLength(100)]
        public string Name { get; set; }
        public string? Description { get; set; }
        public List<Product> Products { get; set; }
    }
}