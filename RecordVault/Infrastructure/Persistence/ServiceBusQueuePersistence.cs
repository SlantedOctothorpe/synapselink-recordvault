using Azure.Messaging.ServiceBus;

using RecordVault.Domain.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Infrastructure.Persistence
{
    public class ServiceBusQueuePersistence(ServiceBusClient serivceBuxClient) : IQueuePersistence
    {
        public async Task EnqueueAsync(string queueName, string message, string sessionId = "")
        {
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));
            if (string.IsNullOrWhiteSpace(message)) throw new ArgumentNullException(nameof(message));

            var serviceBusMessage = new ServiceBusMessage(message);
            if (!string.IsNullOrWhiteSpace(sessionId)) serviceBusMessage.SessionId = sessionId;

            await SendServiceBusMessageAsync(queueName, serviceBusMessage);
        }

        public async Task EnqueueComplexMessageAsync<T>(string queueName, T message)
        {
            ArgumentNullException.ThrowIfNull(message);
            if (string.IsNullOrWhiteSpace(queueName)) throw new ArgumentNullException(nameof(queueName));
            if (message is not ServiceBusMessage) throw new ArgumentException("Message must be of type ServiceBusMessage", nameof(message));

            await SendServiceBusMessageAsync(queueName, message as ServiceBusMessage);
        }

        private async Task SendServiceBusMessageAsync(string queueName, ServiceBusMessage message)
        {
            var sender = serivceBuxClient.CreateSender(queueName);
            await sender.SendMessageAsync(message);
        }
    }
}
