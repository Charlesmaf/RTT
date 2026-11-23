using RTT.Core.Interfaces.Client;
using RTT.Core.Models;
using RTT.Core.Models.Dtos;
using RTT.Service.Helpers;
using RTT.Service.Helpers.Extensions;
using System.Text;

namespace RTT.Service.ClientService
{
    public class ClientService(IClientRepository repository) : IClientService
    {
        public const string CsvHeader =
        "ClientId,Title,FirstName,LastName,Gender,DateOfBirth,IdNumber,AddressType,AddressLine1,AddressLine2,Suburb,City,Province,PostalCode,Country";


        public Task<PagedResult<ClientSummaryDto>> Search(ClientQuery query, CancellationToken cancellationToken)
        {
            query.Page = Math.Max(query.Page, 1);
            query.PageSize = Math.Clamp(query.PageSize, 1, ClientLimits.MaxPageSize);
            query.Search = query.Search?.Trim();

            if (query.Search?.Length > 100)
                query.Search = query.Search[..100];

            return repository.Search(query, cancellationToken);
        }

        public async Task<ClientDto> GetClientById(int clientId, CancellationToken cancellationToken)
        {
            return await repository.GetClientById(clientId, cancellationToken) ?? throw new ClientNotFoundException(clientId);
        }

        public Task<LookupsDto> GetLookups(CancellationToken cancellationToken)
        {
            return repository.GetLookups(cancellationToken);
        }

        public async Task<ClientDto> SaveClient(ClientDto client, CancellationToken cancellationToken)
        {
            client.ClientId = 0;

            var clientId = await repository.Insert(client, cancellationToken);
            return await GetClientById(clientId, cancellationToken);
        }

        public async Task<ClientDto> Update(int clientId, ClientDto client, CancellationToken cancellationToken)
        {
            client.ClientId = clientId;

            if (!IsRowVersion(client.RowVersion))
                throw new ClientValidationException(new() { ["RowVersion"] = "The client's version is missing. Reload the client and try again." });

            if (!await repository.Update(client, cancellationToken))
            {
                if (await repository.GetClientById(clientId, cancellationToken) == null)
                    throw new ClientNotFoundException(clientId);

                throw new ClientConcurrencyException();
            }

            return await GetClientById(clientId, cancellationToken);
        }

        public async Task DeleteClient(int clientId, CancellationToken cancellationToken)
        {
            if (!await repository.DeleteClient(clientId, cancellationToken))
                throw new ClientNotFoundException(clientId);
        }

        public async Task ExportCsv(Stream output, CancellationToken cancellationToken)
        {
            await using var writer = new StreamWriter(output, new UTF8Encoding(true), leaveOpen: true) { NewLine = "\r\n" };
            await writer.WriteLineAsync(CsvHeader);

            await foreach (var row in repository.GetExportRows(cancellationToken))
            {
                string?[] fields =
                [
                    row.ClientId.ToString(), row.Title, row.FirstName, row.LastName, row.Gender,
                    row.DateOfBirth?.ToString("yyyy-MM-dd"), row.IdNumber, row.AddressType, row.AddressLine1, row.AddressLine2,
                    row.Suburb, row.City, row.Province, row.PostalCode, row.Country
                ];

                await writer.WriteLineAsync(string.Join(",", fields.Select(StringExtensions.CsvField)));
            }
        }

        private static bool IsRowVersion(string? value)
        {
            return value != null && Convert.TryFromBase64String(value, new byte[8], out var length) && length == 8;
        }
    }
}
