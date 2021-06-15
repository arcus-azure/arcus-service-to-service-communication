using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Arcus.Shared.Services.Interfaces;
using Arcus.Workers.Orders.Repositories.Interfaces;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.Workers.Orders.Repositories
{
    public class MarketRepository : IMarketRepository
    {
        private readonly ILogger<MarketRepository> _logger;
        private readonly IBaconService _baconService;

        public MarketRepository(IBaconService baconService, ILogger<MarketRepository> logger)
        {
            Guard.NotNull(baconService, nameof(baconService));
            Guard.NotNull(logger, nameof(logger));

            _baconService = baconService;
            _logger = logger;
        }

        public async Task<List<string>> OrderBaconAsync(int amount)
        {
            var bacon = await _baconService.GetBaconAsync();
            
            return bacon.Take(amount).ToList();
        }
    }
}
