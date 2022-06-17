using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Messaging.Abstractions;
using Arcus.Observability.Correlation;
using Arcus.Shared.Services.Interfaces;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Shared.Services
{
    public class BaconService : IBaconService
    {
        private readonly IConfiguration _configuration;
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BaconService> _logger;

        public BaconService(IHttpClientFactory httpClientFactory, ICorrelationInfoAccessor correlationInfoAccessor,
            IConfiguration configuration, ILogger<BaconService> logger)
        {
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor));
            Guard.NotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _correlationInfoAccessor = correlationInfoAccessor;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<string>> GetBaconAsync()
        {
            var url = _configuration["Bacon_API_Url"];

            var requestUri = $"http://{url}/api/v1/bacon";
            //var requestUri = $"http://localhost:789/api/v1/bacon";

            _logger.LogInformation($"Requesting BACON at {requestUri}");

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.SendAndTrackDependencyAsync("Get Bacon", request, _correlationInfoAccessor, _logger);

            _logger.LogInformation("Calling bacon API completed with status:" + response.StatusCode);

            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("Unable to get bacon");
            }

            var rawResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(rawResponse);
        }

        public async Task<List<string>> GetBaconAsync(MessageCorrelationInfo messageCorrelationInfo)
        {
            var url = _configuration["Bacon_API_Url"];

            var requestUri = $"http://{url}/api/v1/bacon";
            //var requestUri = $"http://localhost:789/api/v1/bacon";

            _logger.LogInformation($"Requesting BACON at {requestUri}");

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.SendAndTrackDependencyAsync("Get Bacon", request, messageCorrelationInfo, _logger);

            _logger.LogInformation("Calling bacon API completed with status:" + response.StatusCode);

            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("Unable to get bacon");
            }

            var rawResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(rawResponse);
        }
    }
}