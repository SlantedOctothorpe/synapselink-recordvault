using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;

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
    public class ServiceBusQueueService(ILogger<ServiceBusQueueService> logger, ServiceBusClient serviceBusClient) : IQueueService
    {
        public async Task<T> GetMessageReceiverForSession<T>(string queueName, string sessionId)
        {
            var receiver = await serviceBusClient.AcceptSessionAsync(queueName, sessionId);

            return (T) Convert.ChangeType(receiver, typeof(T));
        }

        public async Task CloseMessageReceivier<T>(T receiver)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionProcessor");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");

            await messageReceiver.CloseAsync();
        }

        public async Task CompleteMessage<T, U>(T receiver, U message)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionProcessor");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");
            var sbMessage = receiver as ServiceBusReceivedMessage ?? throw new ArgumentException("Message cannot be null");

            await messageReceiver.CompleteMessageAsync(sbMessage);
        }

        public async Task DeadLetterMessage<T, U>(T receiver, U message)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionProcessor");
            if (message is not ServiceBusReceivedMessage) throw new ArgumentException("Message is not a ServiceBusReceivedMessage");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");
            var sbMessage = receiver as ServiceBusReceivedMessage ?? throw new ArgumentException("Message cannot be null");

            await messageReceiver.DeadLetterMessageAsync(sbMessage);
        }

        public async Task<IEnumerable<U>> GetDedupedBlobCreatedSessionMessages<T, U>(T receiver)
        {
            if (receiver is not ServiceBusSessionReceiver) throw new ArgumentException("Receiver is not a ServiceBusSessionProcessor");
            var messageReceiver = receiver as ServiceBusSessionReceiver ?? throw new ArgumentException("Receiver cannot be null");

            var messages = new List<U>();
            var uniqueURLs = new HashSet<string>();
            while (true)
            {
                var message = await messageReceiver.ReceiveMessageAsync(TimeSpan.FromSeconds(5));
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
                    await messageReceiver.DeadLetterMessageAsync(message);
                    continue;
                }

                if (uniqueURLs.Contains(blobUrl))
                {
                    logger.LogInformation($"Duplicate message {message.MessageId} - move to dead letter");
                    await messageReceiver.DeadLetterMessageAsync(message);
                    continue;
                }

                uniqueURLs.Add(blobUrl);
                messages.Add((U) Convert.ChangeType(message, typeof(U)));
            }

            return messages;
        }
    }
}
