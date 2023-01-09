using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Shared.Services.Interfaces;
using Arcus.WebApi.Logging.Core.Correlation;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Shared.Services
{
    public class BaconService : IBaconService
    {
        private readonly IConfiguration _configuration;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<BaconService> _logger;

        public BaconService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<BaconService> logger)
        {
            Guard.NotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));

            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<string>> GetBaconAsync()
        {
            var url = _configuration["Bacon_API_Url"];
            var requestUri = $"http://{url}/api/v1/bacon";
            _logger.LogInformation("Requesting BACON at {Uri}", requestUri);

            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);

            var httpClient = _httpClientFactory.CreateClient("Bacon API");
            var response = await httpClient.SendAsync(request);

            _logger.LogInformation("Calling bacon API completed with status: {StatusCode}", response.StatusCode);

            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception($"Unable to get bacon, HTTP status code: {response.StatusCode}");
            }

            var rawResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(rawResponse);
        }
    }
}