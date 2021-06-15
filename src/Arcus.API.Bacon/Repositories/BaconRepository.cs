using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.API.Bacon.Repositories.Interfaces;
using Arcus.Observability.Telemetry.Core;
using Bogus;
using GuardNet;
using Microsoft.Extensions.Logging;

namespace Arcus.API.Bacon.Repositories
{
    public class BaconRepository : IBaconRepository
    {
        private readonly Faker _bogusGenerator = new Faker();

        private readonly ILogger<BaconRepository> _logger;

        public BaconRepository(ILogger<BaconRepository> logger)
        {
            Guard.NotNull(logger, nameof(logger));

            _logger = logger;
        }

        public async Task<List<string>> GetFlavorsAsync()
        {
            using (var dependencyMeasurement = DependencyMeasurement.Start("Get Bacon"))
            {
                try
                {
                    var baconFlavors = new List<string>
                    {
                        "Infamous Black Pepper Bacon",
                        "Italian Bacon",
                        "Raspberry Chipotle",
                        "Pumpkin Pie Spiced",
                        "Apple Cinnamon",
                        "Jalapeño Bacon",
                        "Cajun Style"
                    };

                    await Task.Delay(_bogusGenerator.Random.Int(100, 500));

                    return baconFlavors;
                }
                finally
                {
                    // Normally you would do it in the repo but ok
                    _logger.LogSqlDependency("example-server", "bacon-db", "flavors", isSuccessful:true, dependencyMeasurement);
                }
            }


        }
    }
}
