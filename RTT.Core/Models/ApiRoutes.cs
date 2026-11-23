namespace RTT.Core.Models
{
    public static class ApiRoutes
    {
        public const string Clients = "api/clients";
        public const string ClientsExport = Clients + "/export";
        public const string Lookups = "api/lookups";

        public static string Client(int clientId) => $"{Clients}/{clientId}";
    }
}
