namespace ECommerceAPI.DTOs
{
    public class OrderItemDTO
    {
        public string ProductName { get; set; }
        public decimal ProductPrice { get; set; }
        public int Quantity { get; set; }
        public decimal Discount { get; set; }
        public decimal TotalPrice { get; set; }
    }
}

// To migrate to a database: dotnet ef migrations add setupAdd
//To update to the database: dotnet ef database update