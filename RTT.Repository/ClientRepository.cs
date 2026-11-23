using Microsoft.Data.SqlClient;
using RTT.Core.Interfaces.Client;
using RTT.Core.Models;
using RTT.Core.Models.Dtos;
using System.Data;
using System.Runtime.CompilerServices;

namespace RTT.Repository
{
    public class ClientRepository(string connectionString) : IClientRepository
    {
        private readonly string _connectionString = connectionString;

        public async Task<PagedResult<ClientSummaryDto>> Search(ClientQuery query, CancellationToken cancellationToken)
        {
            const string sql = """
            SELECT COUNT(*)
            FROM dbo.Client
            WHERE @Search IS NULL OR LastName LIKE @Search OR FirstName LIKE @Search OR IdNumber LIKE @Search;

            SELECT c.ClientId, c.Title, c.FirstName, c.LastName, g.Name, c.DateOfBirth, c.IdNumber,
                   (SELECT COUNT(*) FROM dbo.ClientAddress a WHERE a.ClientId = c.ClientId),
                   (SELECT COUNT(*) FROM dbo.ClientContact k WHERE k.ClientId = c.ClientId)
            FROM dbo.Client c
            JOIN dbo.Gender g ON g.GenderId = c.GenderId
            WHERE @Search IS NULL OR c.LastName LIKE @Search OR c.FirstName LIKE @Search OR c.IdNumber LIKE @Search
            ORDER BY c.LastName, c.FirstName, c.ClientId
            OFFSET @Skip ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

            var search = string.IsNullOrWhiteSpace(query.Search) ? null : EscapeLike(query.Search.Trim()) + "%";

            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@Search", SqlDbType.NVarChar, 210).Value = (object?)search ?? DBNull.Value;
            command.Parameters.Add("@Skip", SqlDbType.Int).Value = (query.Page - 1) * query.PageSize;
            command.Parameters.Add("@PageSize", SqlDbType.Int).Value = query.PageSize;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);
            var result = new PagedResult<ClientSummaryDto> { Page = query.Page, PageSize = query.PageSize, TotalCount = reader.GetInt32(0) };

            await reader.NextResultAsync(cancellationToken);
            var items = new List<ClientSummaryDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                items.Add(new ClientSummaryDto
                {
                    ClientId = reader.GetInt32(0),
                    Title = GetString(reader, 1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    Gender = reader.GetString(4),
                    DateOfBirth = GetDate(reader, 5),
                    IdNumber = GetString(reader, 6),
                    AddressCount = reader.GetInt32(7),
                    ContactCount = reader.GetInt32(8)
                });
            }

            result.Items = items;
            return result;
        }

        public async Task<ClientDto?> GetClientById(int clientId, CancellationToken cancellationToken)
        {
            const string sql = """
            SELECT ClientId, Title, FirstName, LastName, GenderId, DateOfBirth, IdNumber, RowVersion, CreatedAtUtc, UpdatedAtUtc
            FROM dbo.Client WHERE ClientId = @ClientId;

            SELECT a.AddressTypeId, t.Name, a.AddressLine1, a.AddressLine2, a.Suburb, a.City, a.Province, a.PostalCode, a.Country
            FROM dbo.ClientAddress a JOIN dbo.AddressType t ON t.AddressTypeId = a.AddressTypeId
            WHERE a.ClientId = @ClientId ORDER BY a.AddressTypeId;

            SELECT k.ContactTypeId, t.Name, k.ContactValue
            FROM dbo.ClientContact k JOIN dbo.ContactType t ON t.ContactTypeId = k.ContactTypeId
            WHERE k.ClientId = @ClientId ORDER BY k.ContactTypeId, k.ClientContactId;
            """;

            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            if (!await reader.ReadAsync(cancellationToken))
                return null;

            var client = new ClientDto
            {
                ClientId = reader.GetInt32(0),
                Title = GetString(reader, 1),
                FirstName = reader.GetString(2),
                LastName = reader.GetString(3),
                GenderId = reader.GetByte(4),
                DateOfBirth = GetDate(reader, 5),
                IdNumber = GetString(reader, 6),
                RowVersion = Convert.ToBase64String((byte[])reader[7]),
                CreatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(8), DateTimeKind.Utc),
                UpdatedAtUtc = DateTime.SpecifyKind(reader.GetDateTime(9), DateTimeKind.Utc)
            };

            await reader.NextResultAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                client.Addresses.Add(new AddressDto
                {
                    AddressTypeId = reader.GetByte(0),
                    AddressTypeName = reader.GetString(1),
                    AddressLine1 = reader.GetString(2),
                    AddressLine2 = GetString(reader, 3),
                    Suburb = GetString(reader, 4),
                    City = reader.GetString(5),
                    Province = GetString(reader, 6),
                    PostalCode = reader.GetString(7),
                    Country = reader.GetString(8)
                });
            }

            await reader.NextResultAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                client.Contacts.Add(new ContactDto
                {
                    ContactTypeId = reader.GetByte(0),
                    ContactTypeName = reader.GetString(1),
                    ContactValue = reader.GetString(2)
                });
            }

            return client;
        }

        public async Task<int> Insert(ClientDto client, CancellationToken cancellationToken)
        {
            const string sql = """
            INSERT INTO dbo.Client (Title, FirstName, LastName, GenderId, DateOfBirth, IdNumber)
                OUTPUT inserted.ClientId
            VALUES (@Title, @FirstName, @LastName, @GenderId, @DateOfBirth, @IdNumber);
            """;

            await using var connection = await OpenConnection(cancellationToken);
            await using var transaction = connection.BeginTransaction();
            await using var command = new SqlCommand(sql, connection, transaction);

            AddClientParameters(command, client);
            client.ClientId = (int)(await command.ExecuteScalarAsync(cancellationToken))!;

            await SaveAddressesAndContactsAsync(transaction, client, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return client.ClientId;
        }

        public async Task<bool> Update(ClientDto client, CancellationToken cancellationToken)
        {
            const string sql = """
            UPDATE dbo.Client
            SET Title = @Title, FirstName = @FirstName, LastName = @LastName, GenderId = @GenderId,
                DateOfBirth = @DateOfBirth, IdNumber = @IdNumber, UpdatedAtUtc = SYSUTCDATETIME()
            WHERE ClientId = @ClientId AND RowVersion = @RowVersion;
            """;

            await using var connection = await OpenConnection(cancellationToken);
            await using var transaction = connection.BeginTransaction();
            await using var command = new SqlCommand(sql, connection, transaction);

            AddClientParameters(command, client);
            command.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
            command.Parameters.Add("@RowVersion", SqlDbType.Binary, 8).Value = Convert.FromBase64String(client.RowVersion!);

            if (await command.ExecuteNonQueryAsync(cancellationToken) == 0)
                return false;

            await SaveAddressesAndContactsAsync(transaction, client, cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return true;
        }

        public async Task<bool> DeleteClient(int clientId, CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand("DELETE FROM dbo.Client WHERE ClientId = @ClientId;", connection);

            command.Parameters.Add("@ClientId", SqlDbType.Int).Value = clientId;

            return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
        }

        public async Task<bool> IdNumberExists(string idNumber, int excludeClientId, CancellationToken cancellationToken)
        {
            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.Client WHERE IdNumber = @IdNumber AND ClientId <> @ClientId;", connection);
            command.Parameters.Add("@IdNumber", SqlDbType.NVarChar, 20).Value = idNumber;
            command.Parameters.Add("@ClientId", SqlDbType.Int).Value = excludeClientId;

            return (int)(await command.ExecuteScalarAsync(cancellationToken))! > 0;
        }

        public async Task<LookupsDto> GetLookups(CancellationToken cancellationToken)
        {
            const string sql = """
            SELECT GenderId, Name FROM dbo.Gender ORDER BY GenderId;
            SELECT AddressTypeId, Name FROM dbo.AddressType ORDER BY AddressTypeId;
            SELECT ContactTypeId, Name, ValueFormat FROM dbo.ContactType ORDER BY ContactTypeId;
            """;

            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var genders = new List<LookupItemDto>();
            while (await reader.ReadAsync(cancellationToken))
            {
                genders.Add(new LookupItemDto { Id = reader.GetByte(0), Name = reader.GetString(1) });
            }

            await reader.NextResultAsync(cancellationToken);
            var addressTypes = new List<LookupItemDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                addressTypes.Add(new LookupItemDto { Id = reader.GetByte(0), Name = reader.GetString(1) });
            }

            await reader.NextResultAsync(cancellationToken);
            var contactTypes = new List<ContactTypeDto>();

            while (await reader.ReadAsync(cancellationToken))
            {
                contactTypes.Add(new ContactTypeDto
                {
                    Id = reader.GetByte(0),
                    Name = reader.GetString(1),
                    ValueFormat = (ContactValueFormat)reader.GetByte(2)
                });
            }

            return new LookupsDto { Genders = genders, AddressTypes = addressTypes, ContactTypes = contactTypes };
        }

        public async IAsyncEnumerable<ClientExportRow> GetExportRows([EnumeratorCancellation] CancellationToken cancellationToken)
        {
            const string sql = """
            SELECT c.ClientId, c.Title, c.FirstName, c.LastName, g.Name, c.DateOfBirth, c.IdNumber,
                   t.Name, a.AddressLine1, a.AddressLine2, a.Suburb, a.City, a.Province, a.PostalCode, a.Country
            FROM dbo.Client c
            JOIN dbo.Gender g ON g.GenderId = c.GenderId
            LEFT JOIN dbo.ClientAddress a ON a.ClientId = c.ClientId
            LEFT JOIN dbo.AddressType t ON t.AddressTypeId = a.AddressTypeId
            ORDER BY c.ClientId, a.AddressTypeId;
            """;

            await using var connection = await OpenConnection(cancellationToken);
            await using var command = new SqlCommand(sql, connection) { CommandTimeout = 600 };
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            while (await reader.ReadAsync(cancellationToken))
            {
                yield return new ClientExportRow
                {
                    ClientId = reader.GetInt32(0),
                    Title = GetString(reader, 1),
                    FirstName = reader.GetString(2),
                    LastName = reader.GetString(3),
                    Gender = reader.GetString(4),
                    DateOfBirth = GetDate(reader, 5),
                    IdNumber = GetString(reader, 6),
                    AddressType = GetString(reader, 7),
                    AddressLine1 = GetString(reader, 8),
                    AddressLine2 = GetString(reader, 9),
                    Suburb = GetString(reader, 10),
                    City = GetString(reader, 11),
                    Province = GetString(reader, 12),
                    PostalCode = GetString(reader, 13),
                    Country = GetString(reader, 14)
                };
            }
        }

        private static async Task SaveAddressesAndContactsAsync(SqlTransaction transaction, ClientDto client, CancellationToken cancellationToken)
        {
            var connection = transaction.Connection!;

            await using (var delete = new SqlCommand("DELETE FROM dbo.ClientAddress WHERE ClientId = @ClientId; DELETE FROM dbo.ClientContact WHERE ClientId = @ClientId;", connection, transaction))
            {
                delete.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                await delete.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var address in client.Addresses)
            {
                await using var insert = new SqlCommand("""
                INSERT INTO dbo.ClientAddress (ClientId, AddressTypeId, AddressLine1, AddressLine2, Suburb, City, Province, PostalCode, Country)
                VALUES (@ClientId, @AddressTypeId, @AddressLine1, @AddressLine2, @Suburb, @City, @Province, @PostalCode, @Country);
                """, connection, transaction);
                insert.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                insert.Parameters.Add("@AddressTypeId", SqlDbType.TinyInt).Value = address.AddressTypeId;
                insert.Parameters.Add("@AddressLine1", SqlDbType.NVarChar, 200).Value = address.AddressLine1;
                insert.Parameters.Add("@AddressLine2", SqlDbType.NVarChar, 200).Value = DbValue(address.AddressLine2);
                insert.Parameters.Add("@Suburb", SqlDbType.NVarChar, 100).Value = DbValue(address.Suburb);
                insert.Parameters.Add("@City", SqlDbType.NVarChar, 100).Value = address.City;
                insert.Parameters.Add("@Province", SqlDbType.NVarChar, 100).Value = DbValue(address.Province);
                insert.Parameters.Add("@PostalCode", SqlDbType.NVarChar, 10).Value = address.PostalCode;
                insert.Parameters.Add("@Country", SqlDbType.NVarChar, 100).Value = address.Country;
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }

            foreach (var contact in client.Contacts)
            {
                await using var insert = new SqlCommand(
                    "INSERT INTO dbo.ClientContact (ClientId, ContactTypeId, ContactValue) VALUES (@ClientId, @ContactTypeId, @ContactValue);",
                    connection, transaction);
                insert.Parameters.Add("@ClientId", SqlDbType.Int).Value = client.ClientId;
                insert.Parameters.Add("@ContactTypeId", SqlDbType.TinyInt).Value = contact.ContactTypeId;
                insert.Parameters.Add("@ContactValue", SqlDbType.NVarChar, 256).Value = contact.ContactValue;
                await insert.ExecuteNonQueryAsync(cancellationToken);
            }
        }

        private static void AddClientParameters(SqlCommand command, ClientDto client)
        {
            command.Parameters.Add("@Title", SqlDbType.NVarChar, 20).Value = DbValue(client.Title);
            command.Parameters.Add("@FirstName", SqlDbType.NVarChar, 100).Value = client.FirstName;
            command.Parameters.Add("@LastName", SqlDbType.NVarChar, 100).Value = client.LastName;
            command.Parameters.Add("@GenderId", SqlDbType.TinyInt).Value = client.GenderId;
            command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = client.DateOfBirth.HasValue ? client.DateOfBirth.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
            command.Parameters.Add("@IdNumber", SqlDbType.NVarChar, 20).Value = DbValue(client.IdNumber);
        }

        private static string EscapeLike(string value)
            => value.Replace("[", "[[]").Replace("%", "[%]").Replace("_", "[_]");

        private static object DbValue(string? value) => value is null ? DBNull.Value : value;

        private static string? GetString(SqlDataReader reader, int column)
            => reader.IsDBNull(column) ? null : reader.GetString(column);

        private static DateOnly? GetDate(SqlDataReader reader, int column)
            => reader.IsDBNull(column) ? null : DateOnly.FromDateTime(reader.GetDateTime(column));

        private async Task<SqlConnection> OpenConnection(CancellationToken cancellationToken)
        {
            var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
    }
}
