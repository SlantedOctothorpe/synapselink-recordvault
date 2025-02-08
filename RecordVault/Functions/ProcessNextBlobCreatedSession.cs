using Azure.Messaging.ServiceBus;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Services;
using RecordVault.DTOs;

using System.Diagnostics;
using System.Text.Json;

namespace RecordVault.Functions
{
    public class ProcessNextBlobCreatedSession(
        ILogger<ProcessNextBlobCreatedSession> logger,
        IEntitySyncService entitySyncService,
        QueueServiceFactory queueServiceFactory
        )
    {
        [Function(nameof(ProcessNextBlobCreatedSession))]
        public async Task Run(
            [ServiceBusTrigger("%AzureStorageBusTriggerProcessQueueName%", Connection = "AzureStorageBusConnectionString")]
            ServiceBusReceivedMessage triggerMessage,
            ServiceBusMessageActions messageActions)
        {
            var processorGuid = Guid.NewGuid();

            var queueService = queueServiceFactory.GetQueueService("servicebus");

            ServiceBusSessionReceiver sessionReceiver = null;
            try
            {
                LogInformationWithGuid(processorGuid, "Starting to process next session");

                var queueName = Environment.GetEnvironmentVariable("AzureStorageBusSessionAwareQueueName") ?? throw new Exception("AzureStorageBusSessionAwareQueueName");

                try {
                    sessionReceiver = await queueService.GetMessageReceiverForNextSessionAsync<ServiceBusSessionReceiver>(queueName);
                } catch (ServiceBusException ex)
                {
                    LogInformationWithGuid(processorGuid, $"No sessions to process");
                    return;
                }

                if (sessionReceiver == null)
                {
                    LogInformationWithGuid(processorGuid, $"No sessions to process");
                    return;
                }

                var sessionLockManager = queueService.StartSessionLockRenewal(sessionReceiver);

                var sessionId = sessionReceiver.SessionId;
                LogInformationWithGuid(processorGuid, $"Starting to process messages for session: {sessionId}");

                var receivedMessages = await queueService.GetDedupedBlobCreatedSessionMessagesAsync<ServiceBusSessionReceiver, ServiceBusReceivedMessage>(sessionReceiver);

                var blobEventList = new List<BlobCreatedEvent>();
                foreach (var message in receivedMessages)
                {
                    try
                    {
                        var blobCreatedEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body);
                        if (blobCreatedEvent == null)
                        {
                            // If it fails to parse, the message may unprocessable
                            LogErrorWithGuid(processorGuid, $"Failed to parse message - {message.MessageId} : {message.Body}");
                            continue;
                        }

                        blobEventList.Add(blobCreatedEvent);
                    }
                    catch
                    {
                        LogErrorWithGuid(processorGuid, $"Failed to parse message - {message.MessageId} : {message.Body}");
                        continue;
                    }
                }

                if (blobEventList.Count != 0)
                {
                    // Convert the blob events to a sync package (each session should be only a single entity)
                    var syncPackages = entitySyncService.BlobCreatedEventsToSyncPackages(blobEventList);

                    var stopwatch = Stopwatch.StartNew();

                    // Sync the schema
                    var sqlCdmTables = await entitySyncService.SyncEntityCDMSchema(syncPackages);

                    stopwatch.Stop();
                    LogInformationWithGuid(processorGuid, $"Schema sync took {stopwatch.ElapsedMilliseconds}ms");

                    stopwatch = Stopwatch.StartNew();

                    // Sync the data
                    await entitySyncService.SyncEntityData(syncPackages, sqlCdmTables);

                    stopwatch.Stop();
                    LogInformationWithGuid(processorGuid, $"Data sync took {stopwatch.ElapsedMilliseconds}ms");

                    var entityCount = syncPackages.Count();
                    LogInformationWithGuid(processorGuid, $"Successfully synced {entityCount} entit(y)(ies)");
                }
                else
                {
                    LogInformationWithGuid(processorGuid, "No messages to process");
                }

                // TODO assume all messages are processed successfully so can be completed
                foreach (var message in receivedMessages)
                {
                    await queueService.CompleteMessageAsync(sessionReceiver, message);
                }

                queueService.StopSessionLockRenewal(sessionLockManager);
            }
            catch (Exception ex)
            {
                LogErrorWithGuid(processorGuid, ex.ToString());

                var stackFrame = (new StackTrace(ex, true)).GetFrame(0);
                LogErrorWithGuid(processorGuid, string.Format("At line {0} column {1} in {2}: {3} {4}{3}{5}  ",
                    stackFrame.GetFileLineNumber(), stackFrame.GetFileColumnNumber(),
                    stackFrame.GetMethod(), Environment.NewLine, stackFrame.GetFileName(),
                    ex.Message));

                return;
            }
            finally
            {
                if (sessionReceiver != null)
                {
                    await queueService.CloseMessageReceiverAsync(sessionReceiver);
                }
            }
        }

        private void LogInformationWithGuid(Guid guid, string message)
        {
            logger.LogInformation("[{guid}] {message}", guid, message);
        }

        private void LogErrorWithGuid(Guid guid, string message)
        {
            logger.LogError("[{guid}] {message}", guid, message);
        }
    }
}
