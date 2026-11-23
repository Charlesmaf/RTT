namespace RTT.Core.Models.Dtos
{
    public class ClientSummaryDto
    {
        public int ClientId { get; set; }
        public string? Title { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Gender { get; set; } = string.Empty;
        public DateOnly? DateOfBirth { get; set; }
        public string? IdNumber { get; set; }
        public int AddressCount { get; set; }
        public int ContactCount { get; set; }
    }
}
