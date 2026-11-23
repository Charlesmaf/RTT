using Microsoft.AspNetCore.Mvc;
using RTT.Core.Models;
using RTT.Core.Models.Dtos;
using RTT.Service.ClientService;

namespace RTT.Service.Controllers
{
    [ApiController]
    [Route(ApiRoutes.Lookups)]
    [Produces("application/json")]
    public class LookupsController(IClientService clientService) : ControllerBase
    {
        [HttpGet]
        [ProducesResponseType<LookupsDto>(StatusCodes.Status200OK)]
        public Task<LookupsDto> Get(CancellationToken cancellationToken)
        {
            return clientService.GetLookups(cancellationToken);
        }
    }
}
