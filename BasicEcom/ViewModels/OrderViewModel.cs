using System.ComponentModel.DataAnnotations;

namespace BasicEcom.ViewModels
{
    // This class is used to represent the order view model to get the missing information from the order
    public class OrderViewModel
    {
        public string? ProductPartitionKey { get; set; }
        public string? ProductRowKey { get; set; }
        [Required, Range(1, int.MaxValue, ErrorMessage = "Invalid Shoe Size")]
        public int Size { get; set; }

        [Required, Range(1, int.MaxValue, ErrorMessage = "Invalid Quantity")]
        public int Quantity { get; set; }

        [Required]
        public string? Colour { get; set; }
    }
}
