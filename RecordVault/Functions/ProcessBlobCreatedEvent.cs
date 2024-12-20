using System.Diagnostics;
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

            try {
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

                var stopwatch = Stopwatch.StartNew();

                // Sync the schema
                var sqlCdmTables = await entitySyncService.SyncEntityCDMSchema(syncPackages);

                stopwatch.Stop();
                logger.LogInformation($"Schema sync took {stopwatch.ElapsedMilliseconds}ms");

                stopwatch = Stopwatch.StartNew();

                // Sync the data
                await entitySyncService.SyncEntityData(syncPackages, sqlCdmTables);

                stopwatch.Stop();
                logger.LogInformation($"Data sync took {stopwatch.ElapsedMilliseconds}ms");

                var entityCount = syncPackages.Count();
                logger.LogInformation($"Successfully synced {entityCount} for {message.MessageId}");

                //// Complete the message
                //await messageActions.CompleteMessageAsync(message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex.ToString());

                var stackFrame = (new StackTrace(ex, true)).GetFrame(0);
                logger.LogError(string.Format("At line {0} column {1} in {2}: {3} {4}{3}{5}  ",
                    stackFrame.GetFileLineNumber(), stackFrame.GetFileColumnNumber(),
                    stackFrame.GetMethod(), Environment.NewLine, stackFrame.GetFileName(),
                    ex.Message));

                throw;
            }
        }
    }
}
