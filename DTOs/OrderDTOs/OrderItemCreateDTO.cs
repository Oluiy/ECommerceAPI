using System.ComponentModel.DataAnnotations;
namespace ECommerceAPI.DTOs
{
    public class OrderItemCreateDTO
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        public int Quantity { get; set; }
    }
}