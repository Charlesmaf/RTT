using Microsoft.Data.SqlClient;

namespace RTT.Repository
{
    public class DatabaseInitializer(string connectionString)
    {
        private readonly string _connectionString = connectionString;

        public async Task Initialize(CancellationToken cancellationToken)
        {
            await CreateDatabase(cancellationToken);
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(cancellationToken);
            await using var command = new SqlCommand(CreateTablesSql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private async Task CreateDatabase(CancellationToken cancellationToken)
        {
            var builder = new SqlConnectionStringBuilder(_connectionString);
            var databaseName = builder.InitialCatalog;

            if (string.IsNullOrWhiteSpace(databaseName))
                throw new InvalidOperationException("The connection string must name a database");

            builder.InitialCatalog = "master";

            await using var connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync(cancellationToken);

            await using var command = new SqlCommand("""
                IF DB_ID(@DatabaseName) IS NULL
                BEGIN
                    DECLARE @Sql NVARCHAR(400) = N'CREATE DATABASE ' + QUOTENAME(@DatabaseName) + N';';
                    EXEC (@Sql);
                END;
                """, connection);
            command.Parameters.Add("@DatabaseName", System.Data.SqlDbType.NVarChar, 128).Value = databaseName;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        private const string CreateTablesSql = """
            
            IF OBJECT_ID('dbo.Gender', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Gender (
                        GenderId TINYINT NOT NULL PRIMARY KEY,
                        Name NVARCHAR (50) NOT NULL
                    );
                END

            IF OBJECT_ID('dbo.AddressType', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.AddressType (
                        AddressTypeId TINYINT NOT NULL PRIMARY KEY,
                        Name NVARCHAR (50) NOT NULL
                    );
                END

            IF OBJECT_ID('dbo.ContactType', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ContactType (
                        ContactTypeId TINYINT NOT NULL PRIMARY KEY,
                        Name NVARCHAR (50) NOT NULL,
                        ValueFormat TINYINT NOT NULL
                    );
                END

            IF OBJECT_ID('dbo.Client', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.Client (
                        ClientId INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                        Title NVARCHAR (20) NULL,
                        FirstName NVARCHAR (100) NOT NULL,
                        LastName NVARCHAR (100) NOT NULL,
                        GenderId TINYINT NOT NULL,
                        DateOfBirth DATE NULL,
                        IdNumber NVARCHAR (20) NULL,
                        RowVersion ROWVERSION NOT NULL,
                        CreatedAtUtc DATETIME2 CONSTRAINT DF_Client_CreatedAtUtc DEFAULT SYSUTCDATETIME() NOT NULL,
                        UpdatedAtUtc DATETIME2 CONSTRAINT DF_Client_UpdatedAtUtc DEFAULT SYSUTCDATETIME() NOT NULL,
                        CONSTRAINT FK_Client_Gender FOREIGN KEY (GenderId) REFERENCES dbo.Gender (GenderId)
                    );
                    CREATE UNIQUE INDEX UX_Client_IdNumber
                        ON dbo.Client(IdNumber) WHERE IdNumber IS NOT NULL;
                    CREATE INDEX IX_Client_Name
                        ON dbo.Client(LastName, FirstName);
                END

            IF OBJECT_ID('dbo.ClientAddress', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ClientAddress (
                        ClientAddressId INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                        ClientId INT NOT NULL,
                        AddressTypeId TINYINT NOT NULL,
                        AddressLine1 NVARCHAR (200) NOT NULL,
                        AddressLine2 NVARCHAR (200) NULL,
                        Suburb NVARCHAR (100) NULL,
                        City NVARCHAR (100) NOT NULL,
                        Province NVARCHAR (100) NULL,
                        PostalCode NVARCHAR (10) NOT NULL,
                        Country NVARCHAR (100) NOT NULL,
                        CONSTRAINT FK_ClientAddress_Client FOREIGN KEY (ClientId) REFERENCES dbo.Client (ClientId) ON DELETE CASCADE,
                        CONSTRAINT FK_ClientAddress_AddressType FOREIGN KEY (AddressTypeId) REFERENCES dbo.AddressType (AddressTypeId),
                        CONSTRAINT UX_ClientAddress_ClientType UNIQUE (ClientId, AddressTypeId)
                    );
                END

            IF OBJECT_ID('dbo.ClientContact', 'U') IS NULL
                BEGIN
                    CREATE TABLE dbo.ClientContact (
                        ClientContactId INT IDENTITY (1, 1) NOT NULL PRIMARY KEY,
                        ClientId INT NOT NULL,
                        ContactTypeId TINYINT NOT NULL,
                        ContactValue NVARCHAR (256) NOT NULL,
                        CONSTRAINT FK_ClientContact_Client FOREIGN KEY (ClientId) REFERENCES dbo.Client (ClientId) ON DELETE CASCADE,
                        CONSTRAINT FK_ClientContact_ContactType FOREIGN KEY (ContactTypeId) REFERENCES dbo.ContactType (ContactTypeId)
                    );
                    CREATE INDEX IX_ClientContact_ClientId
                        ON dbo.ClientContact(ClientId);
                END

            INSERT INTO dbo.Gender (GenderId, Name)
            SELECT v.Id,
                   v.Name
            FROM   (VALUES (1, N'Male'), (2, N'Female'), (3, N'Other')) AS v(Id, Name)
            WHERE  NOT EXISTS (SELECT 1
                               FROM   dbo.Gender AS g
                               WHERE  g.GenderId = v.Id);

            INSERT INTO dbo.AddressType (AddressTypeId, Name)
            SELECT v.Id,
                   v.Name
            FROM   (VALUES (1, N'Residential'), (2, N'Postal'), (3, N'Work')) AS v(Id, Name)
            WHERE  NOT EXISTS (SELECT 1
                               FROM   dbo.AddressType AS a
                               WHERE  a.AddressTypeId = v.Id);

            INSERT INTO dbo.ContactType (ContactTypeId, Name, ValueFormat)
            SELECT v.Id,
                   v.Name,
                   v.ValueFormat
            FROM   (VALUES (1, N'Mobile', 1), (2, N'Home phone', 1), (3, N'Work phone', 1), (4, N'Email', 2)) AS v(Id, Name, ValueFormat)
            WHERE  NOT EXISTS (SELECT 1
                               FROM   dbo.ContactType AS c
                               WHERE  c.ContactTypeId = v.Id);
            
            """;
    }
}
