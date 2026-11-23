using System.ComponentModel.DataAnnotations;

namespace RTT.Core.Models.Dtos
{
    public class ContactDto
    {
        [Range(1, 255, ErrorMessage = "Contact type is required.")]
        public int ContactTypeId { get; set; }

        public string? ContactTypeName { get; set; }

        [Required(ErrorMessage = "A contact value is required.")]
        [StringLength(256)]
        public string ContactValue { get; set; } = string.Empty;
    }
}
