using Azure.Messaging.ServiceBus;
using GuardNet;
using Newtonsoft.Json;
using System;
using System.Text;

namespace Arcus.API.Market.Extensions
{
    [Obsolete("Use Arcus.ServiceBusMessageBuilder instead")]
    public static class ObjectExtensions
    {
        private const string JsonContentType = "application/json";

        /// <summary>
        ///     Creates an Azure Service Bus Message for a message body
        /// </summary>
        /// <param name="messageBody">Body of the Service Bus message to process</param>
        /// <param name="operationId">Unique identifier that spans one operation end-to-end</param>
        /// <param name="transactionId">Unique identifier that spans one or more operations and are considered a transaction/session</param>
        /// <param name="encoding">Encoding to use during serialization. Defaults to UTF8</param>
        /// <returns>Azure Service Bus Message</returns>
        public static ServiceBusMessage AsServiceBusMessage(this object messageBody, string operationId = null, string transactionId = null, string operationParentId = null, Encoding encoding = null)
        {
            Guard.NotNull(messageBody, nameof(messageBody));

            encoding = encoding ?? Encoding.UTF8;

            string serializedMessageBody = JsonConvert.SerializeObject(messageBody);
            byte[] rawMessage = encoding.GetBytes(serializedMessageBody);

            var serviceBusMessage = new ServiceBusMessage(rawMessage)
            {
                ApplicationProperties =
                {
                    { PropertyNames.ContentType, JsonContentType },
                    { PropertyNames.Encoding, encoding.WebName }
                },
                CorrelationId = operationId
            };

            if (string.IsNullOrWhiteSpace(transactionId) == false)
            {
                serviceBusMessage.ApplicationProperties.Add(PropertyNames.TransactionId, transactionId);
            }

            // Contribute Upstream: Annnotating messages with operation information
            if (string.IsNullOrWhiteSpace(operationParentId) == false)
            {
                serviceBusMessage.ApplicationProperties.Add(PropertyNames.OperationParentId, operationParentId);
            }

            return serviceBusMessage;
        }
    }

    public static class PropertyNames
    {
        public const string OperationParentId = "Operation-Parent-Id";
        public const string TransactionId = "Transaction-Id";
        public const string Encoding = "Message-Encoding";
        public const string ContentType = "Content-Type";
    }
}
