using System.Diagnostics;
using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Services;
using RecordVault.DTOs;

namespace RecordVault.Functions
{
    public class ProcessBlobCreatedEventsWithSessions(
        ILogger<ProcessBlobCreatedEventsWithSessions> logger,
        IEntitySyncService entitySyncService,
        IBlobCreatedEventService blobCreatedEventService,
        QueueServiceFactory queueServiceFactory
        )
    {
        [Function(nameof(ProcessBlobCreatedEventsWithSessions))]
        public async Task Run(
            [ServiceBusTrigger("%AzureStorageBusSessionAwareQueueName%", Connection = "AzureStorageBusConnectionString",
                //IsBatched = true, IsSessionsEnabled = true, AutoCompleteMessages = false)]
                IsBatched = true, IsSessionsEnabled = true)]
            ServiceBusReceivedMessage[] receivedMessages,
            ServiceBusSessionMessageActions sessionMessageActions,
            ServiceBusMessageActions messageActions)
        {
            try {
                var sessionId = receivedMessages.Length > 0 ? receivedMessages[0].SessionId : "";
                logger.LogInformation($"Starting to process messages for session: {sessionId} for {receivedMessages.Length} messages");

                var (blobEventList, dedupedMessages) = await blobCreatedEventService.SBMessagesToDeDedupedBlobCreatedEventsAsync(receivedMessages, messageActions);

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

                // TODO assume all messages are processed successfully so can be completed
                //await sessionMessageActions.RenewSessionLockAsync();
                //var queueService = queueServiceFactory.GetQueueService("servicebus");
                //foreach (var message in dedupedMessages)
                //{
                //    await queueService.CompleteMessageAsync(messageActions, message);
                //}
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
