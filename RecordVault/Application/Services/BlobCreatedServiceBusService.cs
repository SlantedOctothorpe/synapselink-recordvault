using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;
using RecordVault.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class BlobCreatedServiceBusService(ILogger<BlobCreatedServiceBusService> logger, QueuePersistenceFactory queuePersistenceFactory) : IBlobCreatedEventService
    {
        public async Task RequeueBlobCreatedWithSession(BlobCreatedEvent blobCreatedEvent)
        {
            ArgumentNullException.ThrowIfNull(blobCreatedEvent);

            var blobURL = blobCreatedEvent.data.blobUrl;
            ArgumentNullException.ThrowIfNull(blobURL);

            var blobStorageURL = new AzureStorageURL(blobURL);

            var sessionId = blobStorageURL.GetBlobParentFolderName();

            // Create a Service Bus message
            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(blobCreatedEvent))
            {
                SessionId = sessionId,
                ContentType = "application/json",
            };

            await SendServiceBusMessage(serviceBusMessage);
        }

        public async Task RequeueBlobCreatedWithSessionAndProperties<T>(BlobCreatedEvent blobCreatedEvent, T originalMessage)
        {
            ArgumentNullException.ThrowIfNull(blobCreatedEvent);
            ArgumentNullException.ThrowIfNull(originalMessage);
            if (originalMessage is not ServiceBusReceivedMessage originalSBMessage) throw new ArgumentException("originalMessage must be of type ServiceBusReceivedMessage");

            var blobURL = blobCreatedEvent.data.blobUrl;
            ArgumentNullException.ThrowIfNull(blobURL);

            var blobStorageURL = new AzureStorageURL(blobURL);

            var sessionId = blobStorageURL.GetBlobParentFolderName();

            // Create a Service Bus message
            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(blobCreatedEvent))
            {
                SessionId = sessionId,
                ContentType = "application/json",
                CorrelationId = originalSBMessage.CorrelationId
            };

            foreach (var property in originalSBMessage.ApplicationProperties)
            {
                serviceBusMessage.ApplicationProperties.Add(property.Key, property.Value);
            }

            await SendServiceBusMessage(serviceBusMessage);
        }

        public async Task<IEnumerable<BlobCreatedEvent>> SBMessagesToDeDedupedBlobCreatedEventsAsync(ServiceBusReceivedMessage[] blobCreatedMessages, ServiceBusMessageActions messageActions)
        {
            ArgumentNullException.ThrowIfNull(blobCreatedMessages);

            if (blobCreatedMessages.Length == 0) return new List<BlobCreatedEvent>();

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");

            var dedupedMessages = new List<BlobCreatedEvent>();
            var uniqueURLs = new HashSet<string>();
            foreach (var message in blobCreatedMessages)
            {
                BlobCreatedEvent? blobCreatedEvent;
                try
                {
                    blobCreatedEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body);
                    if (blobCreatedEvent == null)
                    {
                        // If it fails to parse, the message may unprocessable
                        logger.LogError("Failed to parse message - move to dead letter - {messageId} : {messageBody}", message.MessageId, message.Body);
                        await queuePersistence.DeadLetterMessageAsync(messageActions, message);
                        continue;
                    }
                }
                catch
                {
                    logger.LogError("Failed to parse message - move to dead letter - {messageId} : {messageBody}", message.MessageId, message.Body);
                    await queuePersistence.DeadLetterMessageAsync(messageActions, message);
                    continue;
                }

                var blobURL = blobCreatedEvent.data.blobUrl;
                if (uniqueURLs.Contains(blobURL))
                {
                    logger.LogInformation($"Duplicate message {message.MessageId} - move to dead letter");
                    await queuePersistence.DeadLetterMessageAsync(messageActions, message);
                    continue;
                }

                uniqueURLs.Add(blobURL);
                dedupedMessages.Add(blobCreatedEvent);
            }
            return dedupedMessages;
        }

        #region Private Methods

        private async Task SendServiceBusMessage(ServiceBusMessage message)
        {
            var sessionAwareQueue = Environment.GetEnvironmentVariable("AzureStorageBusSessionAwareQueueName") ?? throw new Exception("SessionAwareQueueName not defined");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            await queuePersistence.EnqueueComplexMessageAsync(sessionAwareQueue, message);
        }

        #endregion
    }
}
