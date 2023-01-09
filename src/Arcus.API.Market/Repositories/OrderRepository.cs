using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Arcus.API.Market.Repositories.Interfaces;
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
        private readonly ILogger<OrderRepository> _logger;
        private readonly ServiceBusSender _serviceBusOrderSender;

        public OrderRepository(IAzureClientFactory<ServiceBusClient> serviceBusClientFactory, ILogger<OrderRepository> logger)
        {
            Guard.NotNull(serviceBusClientFactory, nameof(serviceBusClientFactory));
            Guard.NotNull(logger, nameof(logger));

            ServiceBusClient client = serviceBusClientFactory.CreateClient("Default");
            _serviceBusOrderSender = client.CreateSender("orders");
            _logger = logger;
        }

        public async Task OrderBaconAsync(int amount)
        {
            var orderRequest = new EatBaconRequestMessage
            {
                Amount = amount
            };

            try
            {
                BinaryData data = BinaryData.FromObjectAsJson(orderRequest);
                var serviceBusMessage = new ServiceBusMessage(data);

                await _serviceBusOrderSender.SendMessageAsync(serviceBusMessage);
            }
            finally
            {
                _logger.LogInformation("Done sending at {Time}", DateTimeOffset.UtcNow);
            }
        }
    }
}
