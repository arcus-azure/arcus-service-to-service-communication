using System.Threading.Tasks;
using Arcus.API.Bacon.Repositories.Interfaces;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.API.Bacon.Controllers
{
    /// <summary>
    /// API endpoint to get bacon.
    /// </summary>
    [ApiController]
    [Route("api/v1/bacon")]
    public class BaconController : ControllerBase
    {
        private readonly IBaconRepository _baconRepository;
        private readonly ILogger<BaconController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="BaconController"/> class.
        /// </summary>
        public BaconController(IBaconRepository baconRepository, ILogger<BaconController> logger)
        {
            Guard.NotNull(baconRepository, nameof(baconRepository));
            Guard.NotNull(logger, nameof(logger));

            _baconRepository = baconRepository;
            _logger = logger;
        }

        /// <summary>
        ///     Get Bacon
        /// </summary>
        /// <remarks>Provides an overview of various bacon flavors.</remarks>
        /// <response code="200">Bacon is served!</response>
        /// <response code="503">Uh-oh! Things went wrong</response>
        [HttpGet(Name = "Bacon_Get")]        
        [SwaggerResponseHeader(200, "X-Transaction-ID", "string", "The header that has the transaction ID is used to correlate multiple operation calls")]
        [SwaggerResponseHeader(200, "X-Operation-ID", "string", "The header that has the operation ID is used to uniquely identify this single call")]
        public async Task<IActionResult> Get()
        {
            var baconFlavors = await _baconRepository.GetFlavorsAsync();

            _logger.LogCustomEvent("Bacon Served");
            _logger.LogCustomMetric("Bacon Served", 1);
            
            return Ok(baconFlavors);
        }
    }
}
