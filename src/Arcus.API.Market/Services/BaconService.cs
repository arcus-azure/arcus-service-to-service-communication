using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.API.Market.Services.Interfaces;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.API.Market.Services
{
    public class BaconService : IBaconService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
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
            var apiKey = _configuration["Bacon_API_Key"];

            var request = new HttpRequestMessage(HttpMethod.Get, $"http://{url}/api/v1/bacon");
            request.Headers.Add("X-API-Key", apiKey);

            var response = await SendHttpRequestAsync(request);
            if (response.IsSuccessStatusCode == false)
            {
                throw new Exception("Unable to get bacon");
            }
            
            var rawResponse = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<List<string>>(rawResponse);
        }

        private async Task<HttpResponseMessage> SendHttpRequestAsync(HttpRequestMessage request)
        {
            var httpClient = _httpClientFactory.CreateClient();
            
            var measurement = Stopwatch.StartNew();

            var response = await httpClient.SendAsync(request);
            
            _logger.LogRequest(request, response, measurement.Elapsed);

            return response;
        }
    }
}
