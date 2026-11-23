using RTT.Core.Interfaces.Client;
using RTT.Repository;
using RTT.Service.ClientService;

namespace RTT.Service
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            var connectionString = builder.Configuration.GetConnectionString("ClientManager");

            if (string.IsNullOrWhiteSpace(connectionString))
                throw new InvalidOperationException("Connection string 'ClientManager' is missing from the configuration.");

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddProblemDetails();
            builder.Services.AddSingleton<IClientRepository>(new ClientRepository(connectionString));
            builder.Services.AddScoped<IClientService, ClientService.ClientService>();

            var app = builder.Build();

            await new DatabaseInitializer(connectionString).Initialize(app.Lifetime.ApplicationStopping);

            app.UseExceptionHandler();

            if (app.Environment.IsDevelopment())
                app.MapOpenApi();

            app.UseHttpsRedirection();
            app.UseAuthorization();
            app.MapControllers();

            await app.RunAsync();
        }
    }
}
