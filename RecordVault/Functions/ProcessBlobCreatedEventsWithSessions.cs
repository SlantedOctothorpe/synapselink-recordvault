using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;
using RecordVault.DTOs;

namespace RecordVault.Functions
{
    public class ProcessBlobCreatedEventsWithSessions(
        ILogger<ProcessBlobCreatedEventsWithSessions> logger, IEntitySyncService entitySyncService, IQueueService queueService
        )
    {
        [Function(nameof(ProcessBlobCreatedEventsWithSessions))]
        public async Task Run(
            [ServiceBusTrigger("%AzureStorageBusSessionAwareQueueName%", Connection = "AzureStorageBusConnectionString",
                AutoCompleteMessages = false, IsSessionsEnabled = true)]
            string sessionId,
            ServiceBusSessionProcessor processor)
        {
            logger.LogInformation($"Starting to process messages for session: {sessionId}");

            try {
                var sessionAwareQueueName = Environment.GetEnvironmentVariable("AzureStorageBusSessionAwareQueueName") ?? throw new Exception("SessionAwareQueueName not defined");
                var receiver = await queueService.GetMessageReceiverForSession<ServiceBusSessionReceiver>(sessionAwareQueueName, sessionId);
                var messages = await queueService.GetDedupedBlobCreatedSessionMessages<ServiceBusSessionReceiver, ServiceBusReceivedMessage>(receiver);

                var blobEventList = new List<BlobCreatedEvent>();
                foreach (var message in messages)
                {
                    var blobEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body);
                    if (blobEvent == null)
                    {
                        // If it fails to parse, the message may unprocessable
                        await queueService.DeadLetterMessage(receiver, message);
                        throw new ArgumentException($"Invalid message body {message.MessageId} : {message.Body}");
                    }

                    blobEventList.Add(blobEvent);
                }

                // Convert the blob events to a sync package (each session should be only a single entity)
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
                logger.LogInformation($"Successfully synced {entityCount} entit(y)(ies)");
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
