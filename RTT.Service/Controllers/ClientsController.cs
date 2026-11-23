using Microsoft.AspNetCore.Mvc;
using RTT.Core.Models;
using RTT.Core.Models.Dtos;
using RTT.Service.ClientService;

namespace RTT.Service.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Clients)]
    [Produces("application/json")]
    public class ClientsController(IClientService clientService) : Controller
    {
        [HttpGet]
        public Task<PagedResult<ClientSummaryDto>> Search([FromQuery] ClientQuery query, CancellationToken cancellationToken)
        {
            return clientService.Search(query, cancellationToken);
        }

        [HttpGet("{clientId:int}", Name = nameof(Get))]
        [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public Task<ClientDto> Get(int clientId, CancellationToken cancellationToken)
        {
            return clientService.GetClientById(clientId, cancellationToken);
        }

        [HttpPost]
        [ProducesResponseType<ClientDto>(StatusCodes.Status201Created)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<ClientDto>> Create(ClientDto client, CancellationToken cancellationToken)
        {
            var created = await clientService.SaveClient(client, cancellationToken);
            return CreatedAtRoute(nameof(Get), new { clientId = created.ClientId }, created);
        }

        [HttpPut("{clientId:int}")]
        [ProducesResponseType<ClientDto>(StatusCodes.Status200OK)]
        [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
        public Task<ClientDto> Update(int clientId, ClientDto client, CancellationToken cancellationToken)
        {
            return clientService.Update(clientId, client, cancellationToken);
        }

        [HttpDelete("{clientId:int}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Delete(int clientId, CancellationToken cancellationToken)
        {
            await clientService.DeleteClient(clientId, cancellationToken);
            return NoContent();
        }

        [HttpGet("export")]
        [Produces("text/csv")]
        public async Task Export(CancellationToken cancellationToken)
        {
            Response.ContentType = "text/csv; charset=utf-8";
            Response.Headers.ContentDisposition = $"attachment; filename=\"clients_{DateTime.Now:yyyyMMdd_HHmmss}.csv\"";
            Response.Headers.CacheControl = "no-store";
            await clientService.ExportCsv(Response.Body, cancellationToken);
        }
    }
}
