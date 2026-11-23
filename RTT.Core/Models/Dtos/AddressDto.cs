using System.ComponentModel.DataAnnotations;

namespace RTT.Core.Models.Dtos
{
    public class AddressDto
    {
        [Range(1, 255, ErrorMessage = "Address type is required.")]
        public int AddressTypeId { get; set; }
        public string? AddressTypeName { get; set; }

        [Required(ErrorMessage = "Address line 1 is required.")]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [StringLength(100)]
        public string? Suburb { get; set; }

        [Required(ErrorMessage = "City is required.")]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Province { get; set; }

        [Required(ErrorMessage = "Postal code is required.")]
        [StringLength(10)]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required.")]
        [StringLength(100)]
        public string Country { get; set; } = ClientLimits.DefaultCountry;
    }
}
