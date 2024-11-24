using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;
using RecordVault.DTOs;

namespace RecordVault.Functions
{
    public class ProcessBlobCreatedEvent(ILogger<ProcessBlobCreatedEvent> logger, IEntitySyncService entitySyncService)
    {
        [Function(nameof(ProcessBlobCreatedEvent))]
        public async Task Run(
            [ServiceBusTrigger("record-vault-dev", Connection = "AzureStorageBusConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            logger.LogInformation("Message ID: {id}", message.MessageId);
            logger.LogInformation("Message Body: {body}", message.Body);
            logger.LogInformation("Message Content-Type: {contentType}", message.ContentType);

            // Parse the message body
            var blobEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body);
            if (blobEvent == null)
            {
                // If it fails to parse, the message may unprocessable
                await messageActions.DeadLetterMessageAsync(message);
                throw new ArgumentException($"Invalid message body {message.MessageId} : {message.Body}");
            }

            var blobEventList = new BlobCreatedEvent[] { blobEvent };

            // Convert the blob events to a sync package
            var syncPackages = entitySyncService.BlobCreatedEventsToSyncPackages(blobEventList);

            // Sync the schema
            var sqlCdmTables = await entitySyncService.SyncEntityCDMSchema(syncPackages);

            // Sync the data
            await entitySyncService.SyncEntityData(syncPackages, sqlCdmTables);

            var entityCount = syncPackages.Count();
            logger.LogInformation($"Successfully synced {entityCount} for {message.MessageId}");

            // Complete the message
            await messageActions.CompleteMessageAsync(message);
        }
    }
}
