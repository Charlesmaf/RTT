using RTT.Core.Models;
using RTT.Core.Models.Dtos;

namespace RTT.Service.ClientService
{
    public interface IClientService
    {
        Task DeleteClient(int clientId, CancellationToken cancellationToken);
        Task ExportCsv(Stream destination, CancellationToken cancellationToken);
        Task<ClientDto> GetClientById(int clientId, CancellationToken cancellationToken);
        Task<LookupsDto> GetLookups(CancellationToken cancellationToken);
        Task<ClientDto> SaveClient(ClientDto client, CancellationToken cancellationToken);
        Task<ClientDto> Update(int clientId, ClientDto client, CancellationToken cancellationToken);
        Task<PagedResult<ClientSummaryDto>> Search(ClientQuery query, CancellationToken cancellationToken);
    }
}