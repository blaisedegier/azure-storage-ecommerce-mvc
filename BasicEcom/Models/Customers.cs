using Azure;
using Azure.Data.Tables;
using System.ComponentModel.DataAnnotations;

namespace BasicEcom.Models
{
    // This class is used to store the customer information in the Azure Table Storage
    public class Customers : ITableEntity
    {
        // ITableEntity
        public string? PartitionKey { get; set; }
        public string? RowKey { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; } = ETag.All;

        [Key]
        public string? CustomerID { get; set; }

        [Required, MinLength(3, ErrorMessage = "Invalid Name")]
        public string? Name { get; set; }

        [Required, EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        [Required]
        public string? PasswordHash { get; set; }

        [Required, Phone(ErrorMessage = "Invalid Phone Number"), Length(10, 10, ErrorMessage = "Invalid Phone Number")]
        public string? Phone { get; set; }

        public string? Address { get; set; }

        public bool isAdmin { get; set; } = false;

        // Authorization
        public DateTimeOffset? LastLogin { get; set; }
        public string? SessionToken { get; set; }
    }
}
