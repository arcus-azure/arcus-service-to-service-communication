using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.API.Salads.Repositories.Interfaces;
using Arcus.Observability.Telemetry.Core;
using Bogus;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.API.Salads.Repositories
{
    public class SaladRepository : ISaladRepository
    {
        private readonly Faker _bogusGenerator = new Faker();

        private readonly ILogger<SaladRepository> _logger;

        public SaladRepository(ILogger<SaladRepository> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        public async Task<List<string>> GetDishRecommendationsAsync()
        {
            using (var dependencyMeasurement = DependencyMeasurement.Start("Get Salad Recommendations"))
            {
                try
                {
                    var salads = new List<string>
                    {
                        "Ceasar Salad"
                    };

                    await Task.Delay(_bogusGenerator.Random.Int(100, 500));

                    return salads;
                }
                finally
                {
                    // Normally you would do it in the repo but ok
                    _logger.LogTableStorageDependency("salad-recipes", "salads", isSuccessful:true, dependencyMeasurement);
                }
            }


        }
    }
}
