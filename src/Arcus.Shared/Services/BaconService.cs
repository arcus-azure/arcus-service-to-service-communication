using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Shared.Services.Interfaces;
using GuardNet;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Shared.Services
{
    public class BaconService : IBaconService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly TelemetryClient _telemetryClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaconService> _logger;

        public BaconService(IHttpClientFactory httpClientFactory, TelemetryClient telemetryClient, IConfiguration configuration, ILogger<BaconService> logger)
        {
            Guard.NotNull(httpClientFactory, nameof(httpClientFactory));
            Guard.NotNull(telemetryClient, nameof(telemetryClient));
            Guard.NotNull(configuration, nameof(configuration));
            Guard.NotNull(logger, nameof(logger));
            
            _httpClientFactory = httpClientFactory;
            _telemetryClient = telemetryClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<List<string>> GetBaconAsync()
        {
            var url = _configuration["Bacon_API_Url"];

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{url}/api/v1/bacon");

            var response = await SendHttpRequestAsync("Get Bacon", request);
            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("Unable to get bacon");
            }
            
            var rawResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(rawResponse);
        }

        private async Task<HttpResponseMessage> SendHttpRequestAsync(string operationName, HttpRequestMessage request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            using (_telemetryClient.StartOperation<RequestTelemetry>(operationName))
            {
                var response = await httpClient.SendAsync(request);
                _logger.LogInformation("Calling bacon API completed with status:" + response.StatusCode);
                return response;
            }
            //var measurement = Stopwatch.StartNew();

            //var response = await httpClient.SendAsync(request);
            
            //_logger.LogRequest(request, response, measurement.Elapsed);
        }
    }
}
