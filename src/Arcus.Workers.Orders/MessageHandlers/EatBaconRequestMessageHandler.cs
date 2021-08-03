using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Arcus.POC.Messaging.Abstractions;
using Arcus.POC.Messaging.Pumps.ServiceBus;
using Arcus.Shared.Messages;
using Arcus.Shared.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace Arcus.Workers.Orders.MessageHandlers
{
    public class EatBaconRequestMessageHandler : IAzureServiceBusMessageHandler<EatBaconRequestMessage>
    {
        private readonly IBaconService _baconService;
        private readonly ILogger<EatBaconRequestMessageHandler> _logger;
        
        public EatBaconRequestMessageHandler(IBaconService baconService, ILogger<EatBaconRequestMessageHandler> logger)
        {
            _baconService = baconService;
            _logger = logger;
        }
        
        public async Task ProcessMessageAsync(
            EatBaconRequestMessage message,
            AzureServiceBusMessageContext messageContext,
            MessageCorrelationInfo correlationInfo,
            CancellationToken cancellationToken)
        {
            _logger.LogTrace("Processing message {message}...", message);

            for(int amountOfBaconEaten = 1; amountOfBaconEaten <= message.Amount;amountOfBaconEaten++)
            {
                // TODO: Uncomment
                var bacon = await _baconService.GetBaconAsync();
                _logger.LogInformation("I have just tasted {Bacon} bacon!", bacon.First());
            }

            _logger.LogInformation("Message {message} processed!", message);
        }
    }
}
