using System.ComponentModel.DataAnnotations;
namespace EcommerceAPI.DTOs
{
    // Data Transfer Object for creating a Product.
    public class ProductCreateDTO
    {
        [Required(ErrorMessage = "Product name is required")]
        public string Name { get; set; }
        public string? Description { get; set; }

        [Required(ErrorMessage = "Product CategoryId is required")]
        public int CategoryId { get; set; }

        [Range(0.01, 100000, ErrorMessage = "Price must be between 0.01 and 100000")]
        public decimal Price { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be a Negative value")]
        public int StockQuantity { get; set; }
        
        [Required(ErrorMessage = "DiscountPercentage is Required.")]
        public decimal? DiscountPercentage { get; set; }
        [MaxLength(50)]
        public string SKU { get; set; } 
        
    }
}