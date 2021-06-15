using System.Text;
using System.Threading.Tasks;
using Arcus.API.Market.Repositories.Interfaces;
using Arcus.Shared.Messages;
using GuardNet;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Arcus.API.Market.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly ILogger<OrderRepository> _logger;
        private readonly QueueClient _queueClient;

        public OrderRepository(QueueClient queueClient, ILogger<OrderRepository> logger)
        {
            Guard.NotNull(queueClient, nameof(queueClient));
            Guard.NotNull(logger, nameof(logger));

            _queueClient = queueClient;
            _logger = logger;
        }

        public async Task OrderBaconAsync(int amount)
        {
            var orderRequest = new EatBaconRequestMessage()
            {
                Amount = amount
            };
            await _queueClient.SendAsync(new Message(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(orderRequest))));
        }
    }
}
