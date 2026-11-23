namespace RTT.Service.Helpers
{
    public class ClientValidationException(Dictionary<string, string> errors) : Exception("One or more validation errors occurred.")
    {
        public Dictionary<string, string> Errors { get; } = errors;
    }

    public class ClientNotFoundException(int clientId) : Exception($"Client {clientId} was not found.")
    {
    }

    public class ClientConcurrencyException : Exception
    {
        public ClientConcurrencyException()
            : base("This client was changed by someone else after you opened it. Reload it and make your changes again.")
        {
        }
    }
}
