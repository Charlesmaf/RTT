using RTT.Web.Components;
using RTT.Web.Services.Client;

namespace RTT.Web
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddRazorComponents().AddInteractiveServerComponents();

            var clientServiceUrl = builder.Configuration["ClientService:Url"];

            if (string.IsNullOrWhiteSpace(clientServiceUrl))
                throw new InvalidOperationException("Setting 'ClientService:Url' is missing from the configuration.");

            builder.Services.AddHttpClient<ClientService>(httpClient => httpClient.BaseAddress = new Uri(clientServiceUrl.TrimEnd('/') + "/"));

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }


            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.UseAntiforgery();
            app.MapStaticAssets();

            app.MapGet("/clients/export", async (ClientService clientService, HttpContext httpContext, CancellationToken cancellationToken) =>
            {
                httpContext.Response.ContentType = "text/csv; charset=utf-8";
                httpContext.Response.Headers.ContentDisposition = $"attachment; filename=\"clients_{DateTime.Now:yyyyMMdd_HHmmss}.csv\"";
                await clientService.Export(httpContext.Response.Body, cancellationToken);
            });

            app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
