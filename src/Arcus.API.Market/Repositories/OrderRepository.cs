using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.API.Market.Extensions;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.Observability.Telemetry.Core.Logging;
using Arcus.Shared.Messages;
using GuardNet;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Azure;
using Microsoft.Azure.ServiceBus;

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
            var client = serviceBusClientFactory.CreateClient("Default");
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
                var newOperationId = $"operation-{Guid.NewGuid()}";
                var newDependencyId = Guid.NewGuid().ToString();

                try
                {
                    var serviceBusMessage = ServiceBusMessageBuilder.CreateForBody(orderRequest)
                                                                    .WithOperationId(newOperationId)
                                                                    .WithTransactionId(correlationInfo?.TransactionId)
                                                                    .WithOperationParentId(newDependencyId)
                                                                    .Build();

                    await _serviceBusOrderSender.SendMessageAsync(serviceBusMessage);

                    isSuccessful = true;
                }
                finally
                {
                    var serviceBusEndpoint = _serviceBusOrderSender.FullyQualifiedNamespace;
                    string entityPath = _serviceBusOrderSender.EntityPath;
                    _logger.LogInformation($"Done sending at {DateTimeOffset.UtcNow}");
                    //_logger.LogServiceBusQueueDependency(serviceBusEndpoint, entityPath, isSuccessful, serviceBusDependencyMeasurement, dependencyId: newDependencyId);
                    _logger.LogWarning(MessageFormats.DependencyFormat, new DependencyLogEntry(
                        "Azure Service Bus",
                        entityPath,
                        null,
                        entityPath,
                        newDependencyId,
                        serviceBusDependencyMeasurement.Elapsed,
                        serviceBusDependencyMeasurement.StartTime,
                        null,
                        isSuccessful,
                        new Dictionary<string, object>()));
                }
            }
        }
    }
}
