using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ECommerceAPI.Models;

namespace ECommerceAPI.Models
{
    public class Product
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Product name is required")]
        [Range(minimum:2, maximum:255)]
        [StringLength(255)]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }
        
        [Column(TypeName = "decimal(18,2)")]
        public decimal? DiscountPercentage { get; set; } //Product Level Discount

        [StringLength(250)]
        public string? Description { get; set; } = "";
        
        [MaxLength(50)]
        public string SKU { get; set; } 
        
        [Required]
        public int StockQuantity { get; set; }

        [Required(ErrorMessage = "Product Category is required")]
        [StringLength(150)]
        public int CategoryId { get; set; } // Foreign Key

        [ForeignKey("CategoryId")] 
        public Category Category { get; set; }
    }
}
