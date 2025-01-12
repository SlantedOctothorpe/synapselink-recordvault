using Azure.Messaging.ServiceBus;

using RecordVault.Domain.Persistence;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Infrastructure.Persistence
{
    public class ServiceBusQueuePersistence(ServiceBusClient serivceBusClient) : IQueuePersistence
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

        public async Task<T> CreateMessageReceiverForSessionAsync<T>(string queueName, string sessionId)
        {
            var receiver = await serivceBusClient.AcceptSessionAsync(queueName, sessionId);

            return (T) Convert.ChangeType(receiver, typeof(T));
        }

        public async Task CloseMessageReceiverAsync<T>(T receiver)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");

            await messageReceiver.CloseAsync();
        }

        public async Task<U> ReceiveMessageAsync<T, U>(T receiver, TimeSpan? maxWaitTime = null)
        {
            // TODO all methods written for ServiceBusSessionReceiver should be refactored to expect ServiceBusReceiver
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");

            var waitTime = maxWaitTime ?? TimeSpan.FromSeconds(10);
            var message = await messageReceiver.ReceiveMessageAsync(waitTime);
            var retMessage = (U) Convert.ChangeType(message, typeof(U));
            return retMessage;
        }

        public async Task CompleteMessageAsync<T, U>(T receiver, U message)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");
            var sbMessage = receiver as ServiceBusReceivedMessage ?? throw new ArgumentException("Message cannot be null");

            await messageReceiver.CompleteMessageAsync(sbMessage);
        }

        public async Task DeadLetterMessageAsync<T, U>(T receiver, U message)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");
            var sbMessage = receiver as ServiceBusReceivedMessage ?? throw new ArgumentException("Message cannot be null");

            await messageReceiver.DeadLetterMessageAsync(sbMessage);
        }

        #region Private Methods

        private async Task SendServiceBusMessageAsync(string queueName, ServiceBusMessage message)
        {
            var sender = serivceBusClient.CreateSender(queueName);
            await sender.SendMessageAsync(message);
        }

        #endregion
    }
}
