using System.ComponentModel.DataAnnotations;

namespace RTT.Core.Models.Dtos
{
    public class ClientDto
    {
        public int ClientId { get; set; }

        [StringLength(20)]
        public string? Title { get; set; }

        [Required(ErrorMessage = "First name is required.")]
        [StringLength(100)]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Last name is required.")]
        [StringLength(100)]
        public string LastName { get; set; } = string.Empty;

        [Range(1, 255, ErrorMessage = "Gender is required.")]
        public int GenderId { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [StringLength(20)]
        public string? IdNumber { get; set; }

        public string? RowVersion { get; set; }

        public DateTime? CreatedAtUtc { get; set; }

        public DateTime? UpdatedAtUtc { get; set; }

        [MaxLength(ClientLimits.MaxAddresses, ErrorMessage = "A client can have at most 10 addresses.")]
        public List<AddressDto> Addresses { get; set; } = [];

        [MaxLength(ClientLimits.MaxContacts, ErrorMessage = "A client can have at most 20 contact details.")]
        public List<ContactDto> Contacts { get; set; } = [];
    }
}
