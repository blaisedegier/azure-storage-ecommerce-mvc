using System.ComponentModel.DataAnnotations;

/*
 * Code Attribution:
 * Add server side validation for uploaded files
 * muhammadelhelaly
 * 26 August 2023
 * GitHub
 * https://github.com/muhammadelhelaly/GameZone/blob/master/GameZone/Attributes/AllowedExtensionsAttribute.cs
 */
namespace BasicEcom.Attributes
{
    // This class is used to validate the file extension of the uploaded file.
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public class AllowedExtensionsAttribute : ValidationAttribute
    {
        private readonly string[] _extensions;
        public AllowedExtensionsAttribute(string[] extensions)
        {
            _extensions = extensions;
        }

        protected override ValidationResult IsValid(object? value, ValidationContext validationContext)
        {
            var file = value as IFormFile;
            if (file != null)
            {
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                if (!_extensions.Contains(extension))
                {
                    return new ValidationResult($"Only {string.Join(", ", _extensions)} files are allowed.");
                }
            }

            return ValidationResult.Success!;
        }
    }
}
