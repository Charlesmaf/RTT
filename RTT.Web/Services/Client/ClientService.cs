using Microsoft.AspNetCore.WebUtilities;
using RTT.Core.Models;
using RTT.Core.Models.Dtos;

namespace RTT.Web.Services.Client
{
    public class ClientService(HttpClient _httpClient)
    {
        private const string errorMessage = "The client API cannot be reached. An error occurred while communicating with the API.";

        public async Task<PagedResult<ClientSummaryDto>> Search(ClientQuery query, CancellationToken cancellationToken)
        {
            var parameters = new Dictionary<string, string?>
            {
                ["search"] = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search,
                ["page"] = query.Page.ToString(),
                ["pageSize"] = query.PageSize.ToString()
            };

            var url = QueryHelpers.AddQueryString(ApiRoutes.Clients, parameters);

            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(url);

                using (response)
                {
                    response.EnsureSuccessStatusCode();
                    var page = await response.Content.ReadFromJsonAsync<PagedResult<ClientSummaryDto>>(cancellationToken: cancellationToken);
                    return page;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task<LookupsDto> GetLookupsAsync()
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(ApiRoutes.Lookups);

                using (response)
                {
                    response.EnsureSuccessStatusCode();
                    var lookups = await response.Content.ReadFromJsonAsync<LookupsDto>();
                    return lookups!;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task<ClientDto> GetClient(int clientId)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(ApiRoutes.Client(clientId));

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        HttpValidationProblemDetails? problem = null;

                        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                            problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

                        var message = problem?.Detail ?? problem?.Title ?? "Something went wrong. Please try again.";
                        throw new HttpRequestException(message, null, response.StatusCode);
                    }

                    var client = await response.Content.ReadFromJsonAsync<ClientDto>();
                    return client!;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task<ClientDto> SaveClient(ClientDto client)
        {
            HttpResponseMessage response;

            try
            {
                if (client.ClientId == 0)
                    response = await _httpClient.PostAsJsonAsync(ApiRoutes.Clients, client);
                else
                    response = await _httpClient.PutAsJsonAsync(ApiRoutes.Client(client.ClientId), client);

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        HttpValidationProblemDetails? problem = null;

                        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                            problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

                        var message = problem?.Detail ?? problem?.Title ?? "Something went wrong. Please try again.";
                        var error = new HttpRequestException(message, null, response.StatusCode);

                        if (problem is not null)
                        {
                            foreach (var field in problem.Errors)
                            {
                                if (field.Value.Length > 0)
                                    error.Data[field.Key] = field.Value[0];
                            }
                        }

                        throw error;
                    }

                    var saved = await response.Content.ReadFromJsonAsync<ClientDto>();
                    return saved!;
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task DeleteClient(int clientId, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.DeleteAsync(ApiRoutes.Client(clientId), cancellationToken);

                using (response)
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        HttpValidationProblemDetails? problem = null;

                        if (response.Content.Headers.ContentType?.MediaType == "application/problem+json")
                            problem = await response.Content.ReadFromJsonAsync<HttpValidationProblemDetails>();

                        var message = problem?.Detail ?? problem?.Title ?? "Something went wrong. Please try again.";

                        throw new HttpRequestException(message, null, response.StatusCode);
                    }
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }

        public async Task Export(Stream destination, CancellationToken cancellationToken)
        {
            HttpResponseMessage response;

            try
            {
                response = await _httpClient.GetAsync(ApiRoutes.ClientsExport, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

                using (response)
                {
                    response.EnsureSuccessStatusCode();
                    await using var csv = await response.Content.ReadAsStreamAsync(cancellationToken);

                    await csv.CopyToAsync(destination, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(errorMessage, ex);
            }
        }
    }
}
