using System;
using System.Text;
using System.Threading.Tasks;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Observability.Correlation;
using Arcus.Observability.Telemetry.Core;
using Arcus.POC.Observability.Telemetry.Serilog.Sinks.ApplicationInsights.Extensions;
using Arcus.Shared.Messages;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.API.Market.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ICorrelationInfoAccessor _correlationInfoAccessor;
        private readonly ILogger<OrderRepository> _logger;
        private readonly QueueClient _queueClient;

        public OrderRepository(QueueClient queueClient, ICorrelationInfoAccessor correlationInfoAccessor, ILogger<OrderRepository> logger)
        {
            Guard.NotNull(correlationInfoAccessor, nameof(correlationInfoAccessor));
            Guard.NotNull(queueClient, nameof(queueClient));
            Guard.NotNull(logger, nameof(logger));

            _correlationInfoAccessor = correlationInfoAccessor;
            _queueClient = queueClient;
            _logger = logger;
        }

        public async Task OrderBaconAsync(int amount)
        {
            var orderRequest = new EatBaconRequestMessage
            {
                Amount = amount
            };

            using (var serviceBusDependencyMeasurement = DependencyMeasurement.Start("Order Bacon"))
            {
                bool isSuccessful = false;
                var correlationInfo = _correlationInfoAccessor.GetCorrelationInfo();
                var newDependencyId = Guid.NewGuid().ToString();
                var upstreamOperationParentId = $"|{correlationInfo?.OperationId}.{newDependencyId}";

                try
                {
                    var serviceBusMessage = orderRequest.AsServiceBusMessage(operationId: correlationInfo?.OperationId, transactionId: correlationInfo?.TransactionId, operationParentId: upstreamOperationParentId);
                    
                    await _queueClient.SendAsync(serviceBusMessage);

                    isSuccessful = true;
                }
                finally
                {
                    // TODO: Support linking as well
                    _logger.LogExtendedServiceBusQueueDependency(_queueClient.QueueName, isSuccessful, serviceBusDependencyMeasurement, dependencyId: upstreamOperationParentId);
                }                
            }
        }
    }
}
