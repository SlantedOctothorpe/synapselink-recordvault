using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using RecordVault.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class ServiceBusQueueService(ILogger<ServiceBusQueueService> logger, QueuePersistenceFactory queuePersistenceFactory) : IQueueService
    {
        public Task EnqueueAsync(string queueName, string message, string sessionId = "")
        {
            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            return queuePersistence.EnqueueAsync(queueName, message, sessionId);
        }

        public Task EnqueueComplexMessageAsync<T>(string queueName, T message)
        {
            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            return queuePersistence.EnqueueComplexMessageAsync(queueName, message);
        }

        public async Task<T> GetMessageReceiverForSessionAsync<T>(string queueName, string sessionId)
        {
            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            var receiver = await queuePersistence.CreateMessageReceiverForSessionAsync<T>(queueName, sessionId);

            return receiver;
        }

        public async Task<T> GetMessageReceiverForNextSessionAsync<T>(string queueName)
        {
            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            var receiver = await queuePersistence.CreateMessageReceiverForNextSessionAsync<T>(queueName);

            return receiver;
        }

        public async Task CloseMessageReceiverAsync<T>(T receiver)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            await queuePersistence.CloseMessageReceiverAsync(receiver);
        }

        public async Task CompleteMessageAsync<T, U>(T receiver, U message)
        {
            if (receiver is not (ServiceBusSessionReceiver or ServiceBusMessageActions)) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver or ServiceBusMessageActions");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            await queuePersistence.CompleteMessageAsync(receiver, message);
        }

        public async Task DeadLetterMessageAsync<T, U>(T receiver, U message)
        {
            if (receiver is not (ServiceBusSessionReceiver or ServiceBusMessageActions)) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver or ServiceBusMessageActions");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            await queuePersistence.DeadLetterMessageAsync(receiver, message);
        }

        public async Task<IEnumerable<U>> GetDedupedBlobCreatedSessionMessagesAsync<T, U>(T receiver)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionReceiver");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");

            var messages = new List<U>();
            var uniqueURLs = new HashSet<string>();
            while (true)
            {
                var message = await queuePersistence.ReceiveMessageAsync<ServiceBusSessionReceiver, ServiceBusReceivedMessage>(messageReceiver, TimeSpan.FromSeconds(10));
                if (message == null) break; // No more messages
                string? blobUrl;
                try
                {
                    var messageObj = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body) ?? throw new Exception();
                    blobUrl = messageObj.data.url;
                }
                catch
                {
                    logger.LogError("Failed to parse message - move to dead letter - {messageId} : {messageBody}", message.MessageId, message.Body);
                    await queuePersistence.DeadLetterMessageAsync(messageReceiver, message);
                    continue;
                }

                if (uniqueURLs.Contains(blobUrl))
                {
                    logger.LogInformation($"Duplicate message {message.MessageId} - move to dead letter");
                    await queuePersistence.DeadLetterMessageAsync(messageReceiver, message);
                    continue;
                }

                uniqueURLs.Add(blobUrl);
                messages.Add((U) Convert.ChangeType(message, typeof(U)));
            }

            return messages;
        }
    }
}
