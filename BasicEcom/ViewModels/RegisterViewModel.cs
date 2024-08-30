using System.ComponentModel.DataAnnotations;

namespace BasicEcom.ViewModels
{
    // This class is used to validate the user input for the registration form.
    public class RegisterViewModel
    {
        [Required, MinLength(3, ErrorMessage = "Invalid Name")]
        public string? Name { get; set; }

        [Required, EmailAddress(ErrorMessage = "Invalid Email Address")]
        public string? Email { get; set; }

        [Required, DataType(DataType.Password)]
        public string? Password { get; set; }

        [Required, DataType(DataType.Password), Compare("Password", ErrorMessage = "Passwords do not match.")]
        public string? ConfirmPassword { get; set; }

        [Required, Phone, StringLength(10, MinimumLength = 10, ErrorMessage = "Invalid Phone Number")]
        public string? Phone { get; set; }

        public string? Address { get; set; }
    }
}
