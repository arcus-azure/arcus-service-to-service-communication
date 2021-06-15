using System.Threading.Tasks;
using Arcus.API.Market.Contracts;
using Arcus.API.Market.Repositories.Interfaces;
using GuardNet;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Filters;

namespace Arcus.API.Market.Controllers
{
    /// <summary>
    /// API endpoint related to the market.
    /// </summary>
    [ApiController]
    [Route("api/v1/market")]
    public class MarketController : ControllerBase
    {
        private readonly IMarketRepository _marketRepository;
        private readonly ILogger<MarketController> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="MarketController"/> class.
        /// </summary>
        public MarketController(IMarketRepository marketRepository, ILogger<MarketController> logger)
        {
            Guard.NotNull(marketRepository, nameof(marketRepository));
            Guard.NotNull(logger, nameof(logger));

            _marketRepository = marketRepository;
            _logger = logger;
        }

        /// <summary>
        ///     Create Order
        /// </summary>
        /// <remarks>Provides capability to create an order in the marketplace.</remarks>
        /// <response code="201">Order is created</response>
        /// <response code="503">Uh-oh! Things went wrong</response>
        [HttpPost(Name = "Market_CreateOrder")]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(HealthReport), StatusCodes.Status503ServiceUnavailable)]
        [SwaggerResponseHeader(201, "RequestId", "string", "The header that has a request ID that uniquely identifies this operation call")]
        [SwaggerResponseHeader(201, "X-Transaction-Id", "string", "The header that has the transaction ID is used to correlate multiple operation calls.")]
        public async Task<IActionResult> CreateOrder([FromBody] OrderRequest orderRequest)
        {
            var saladFlavors = await _marketRepository.OrderBaconAsync(orderRequest.Amount);

            _logger.LogEvent("Order Created");
            _logger.LogMetric("Order Created", 1);
            
            return Ok(saladFlavors);
        }
    }
}
