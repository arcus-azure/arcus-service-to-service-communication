using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Threading.Tasks;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Shared.Services.Interfaces;
using GuardNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.Shared.Services
{
    public class BaconService : IBaconService
    {
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaconService> _logger;

        public BaconService(IHttpClientFactory httpClientFactory, ICorrelationInfoAccessor correlationInfoAccessor, IConfiguration configuration, ILogger<BaconService> logger)
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

            using(var httpDependencyMeasurement = DependencyMeasurement.Start(operationName))
            {
                // TODO: Verify
                var correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                request.Headers.Add("Request-Id", $"|{correlationInfo?.OperationId}");
                request.Headers.Add("X-Transaction-ID", correlationInfo?.TransactionId);

                // TODO: Check HTTP pipeline for hooks
                // https://thomaslevesque.com/2016/12/08/fun-with-the-httpclient-pipeline/
                var response = await httpClient.SendAsync(request);
                
                _logger.LogInformation("Calling bacon API completed with status:" + response.StatusCode);
                _logger.LogHttpDependency(request, response.StatusCode, httpDependencyMeasurement);

                return response;
            }
        }
    }
}
