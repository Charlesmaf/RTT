namespace RTT.Core.Models.Dtos
{
    public class ContactTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public ContactValueFormat ValueFormat { get; set; }
    }
}
