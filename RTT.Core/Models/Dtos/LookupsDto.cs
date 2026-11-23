namespace RTT.Core.Models.Dtos
{
    public class LookupsDto
    {
        public List<LookupItemDto> Genders { get; set; } = [];
        public List<LookupItemDto> AddressTypes { get; set; } = [];
        public List<ContactTypeDto> ContactTypes { get; set; } = [];
    }
}
