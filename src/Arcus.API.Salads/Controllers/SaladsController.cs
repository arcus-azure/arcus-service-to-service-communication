using System.Threading.Tasks;
using Arcus.API.Salads.Repositories.Interfaces;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.API.Salads.Controllers
{
    /// <summary>
    /// API endpoint to get salads.
    /// </summary>
    [ApiController]
    [Route("api/v1/salads")]
    public class SaladsController : ControllerBase
    {
        private readonly ISaladRepository _saladRepository;
        private readonly ILogger<SaladsController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaladsController"/> class.
        /// </summary>
        public SaladsController(ISaladRepository saladRepository, ILogger<SaladsController> logger)
        {
            Guard.NotNull(saladRepository, nameof(saladRepository));
            Guard.NotNull(logger, nameof(logger));

            _saladRepository = saladRepository;
            _logger = logger;
        }

        /// <summary>
        ///     Get Salad
        /// </summary>
        /// <remarks>Provides an overview of salad dishes.</remarks>
        /// <response code="200">Salad is served!</response>
        /// <response code="503">Uh-oh! Things went wrong</response>
        [HttpGet(Name = "Salad_Get")]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status503ServiceUnavailable)]
        [SwaggerResponseHeader(200, "RequestId", "string", "The header that has a request ID that uniquely identifies this operation call")]
        [SwaggerResponseHeader(200, "X-Transaction-Id", "string", "The header that has the transaction ID is used to correlate multiple operation calls.")]
        public async Task<IActionResult> Get()
        {
            var saladFlavors = await _saladRepository.GetDishRecommendationsAsync();

            _logger.LogEvent("Salad Served");
            _logger.LogMetric("Salad Served", 1);
            
            return Ok(saladFlavors);
        }
    }
}
