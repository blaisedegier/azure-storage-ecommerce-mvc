namespace BasicEcom.Models
{
    // This class represents the invoice details, an interface for the pdf generation
    public class Invoice
    {
        public string Title { get; set; } = "Sneaker Store";
        // Customer Details
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime OrderDate { get; set; }

        // Product Details
        public string? ProductName { get; set; }
        public string? Description { get; set; }
        public double Price { get; set; }
        public int Size { get; set; }
        public int Quantity { get; set; }
        public string? Colour { get; set; }
        public double TotalAmount => Price * Quantity;
    }
}
