using Azure.Messaging.ServiceBus;

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
    public class BlobCreatedServiceBusService(QueuePersistenceFactory queuePersistenceFactory) : IBlobCreatedEventService
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

        private async Task SendServiceBusMessage(ServiceBusMessage message)
        {
            var sessionAwareQueue = Environment.GetEnvironmentVariable("AzureStorageBusSessionAwareQueueName") ?? throw new Exception("SessionAwareQueueName not defined");

            var queuePersistence = queuePersistenceFactory.GetQueuePersistence("servicebus");
            await queuePersistence.EnqueueComplexMessageAsync(sessionAwareQueue, message);
        }
    }
}
