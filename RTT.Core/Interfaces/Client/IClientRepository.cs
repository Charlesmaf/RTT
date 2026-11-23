using RTT.Core.Models;
using RTT.Core.Models.Dtos;

namespace RTT.Core.Interfaces.Client
{
    public interface IClientRepository
    {
        Task<PagedResult<ClientSummaryDto>> Search(ClientQuery query, CancellationToken cancellationToken);
        Task<ClientDto?> GetClientById(int clientId, CancellationToken cancellationToken);
        Task<int> Insert(ClientDto client, CancellationToken cancellationToken);
        Task<bool> Update(ClientDto client, CancellationToken cancellationToken);
        Task<bool> DeleteClient(int clientId, CancellationToken cancellationToken);
        Task<bool> IdNumberExists(string idNumber, int excludeClientId, CancellationToken cancellationToken);
        Task<LookupsDto> GetLookups(CancellationToken cancellationToken);
        IAsyncEnumerable<ClientExportRow> GetExportRows(CancellationToken cancellationToken);
    }
}
