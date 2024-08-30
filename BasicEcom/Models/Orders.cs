using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace BasicEcom.Models
{
    // This class is used to create the Orders table in the Azure Table Storage
    public class Orders : ITableEntity
    {
        // ITableEntity
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Key]
        public string? OrderID { get; set; }
        // Foreign Keys
        public string? CustomerID { get; set; }
        public string? ProductID { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Invalid Shoe Size")]
        public int Size { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Invalid Quantity")]
        public int Quantity { get; set; }

        [Required]
        public string? Colour { get; set; }
    }
}
