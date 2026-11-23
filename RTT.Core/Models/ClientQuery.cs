using System.ComponentModel.DataAnnotations;

namespace RTT.Core.Models
{
    public class ClientQuery
    {
        [StringLength(100)]
        public string? Search { get; set; }

        [Range(1, 1_000_000)]
        public int Page { get; set; } = 1;

        [Range(1, ClientLimits.MaxPageSize)]
        public int PageSize { get; set; } = 25;
    }
}
