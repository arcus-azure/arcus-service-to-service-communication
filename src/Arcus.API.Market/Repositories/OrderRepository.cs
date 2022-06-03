using System;
using System.Threading.Tasks;
using Arcus.API.Market.Extensions;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Shared.Messages;
using GuardNet;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;

namespace Arcus.API.Market.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger<OrderRepository> _logger;
        private readonly ServiceBusSender _serviceBusOrderSender;

        public OrderRepository(IAzureClientFactory<ServiceBusClient> serviceBusClientFactory, ICorrelationInfoAccessor correlationInfoAccessor, ILogger<OrderRepository> logger)
        {
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor));
            Guard.NotNull(serviceBusClientFactory, nameof(serviceBusClientFactory));
            Guard.NotNull(logger, nameof(logger));

            _correlationInfoAccessor = correlationInfoAccessor;
            var client = serviceBusClientFactory.CreateClient("orderclient");
            _serviceBusOrderSender = client.CreateSender("orders");
            _logger = logger;
        }

        public async Task OrderBaconAsync(int amount)
        {
            var orderRequest = new EatBaconRequestMessage
            {
                Amount = amount
            };

            using (var serviceBusDependencyMeasurement = DurationMeasurement.Start())
            {
                bool isSuccessful = false;
                var correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                var newDependencyId = Guid.NewGuid().ToString();
                var upstreamOperationParentId = $"|{correlationInfo?.OperationId}.{newDependencyId}";

                try
                {
                    var serviceBusMessage = orderRequest.AsServiceBusMessage(operationId: correlationInfo?.OperationId, transactionId: correlationInfo?.TransactionId, operationParentId: upstreamOperationParentId);

                    await _serviceBusOrderSender.SendMessageAsync(serviceBusMessage);

                    isSuccessful = true;
                }
                finally
                {
                    // TODO: Support linking as well
                    var serviceBusEndpoint = _serviceBusOrderSender.FullyQualifiedNamespace;
                    _logger.LogInformation($"Done sending at {DateTimeOffset.UtcNow}");
                    _logger.LogServiceBusQueueDependency(_serviceBusOrderSender.EntityPath, isSuccessful, serviceBusDependencyMeasurement, dependencyId: upstreamOperationParentId);
                }
            }
        }
    }
}
