using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace BasicEcom.Models
{
    // A product row in the products table
    public class Products : ITableEntity
    {
        // ITableEntity
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Key]
        public string? ProductID { get; set; }

        [Required, MinLength(2, ErrorMessage = "Invalid Name")]
        public string? Name { get; set; }

        [Required, MinLength(10, ErrorMessage = "Description Too Short")]
        public string? Description { get; set; }

        [Required, Range(0.0, double.MaxValue, ErrorMessage = "Invalid Price")]
        public double Price { get; set; }

        // Name of the product image in the blob container, or null when there is no image.
        public string? ImageUrl { get; set; }
    }
}
