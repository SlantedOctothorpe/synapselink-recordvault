using System;
using System.Diagnostics;
using System.Text.Json;
using System.Threading.Tasks;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;
using RecordVault.DTOs;

namespace RecordVault.Functions
{
    public class AddSessionToBlobCreatedEvent(ILogger<AddSessionToBlobCreatedEvent> logger, IBlobCreatedEventService blobCreatedEventService)
    {
        [Function(nameof(AddSessionToBlobCreatedEvent))]
        public async Task Run(
            [ServiceBusTrigger("%AzureStorageBusPreSessionQueueName%", Connection = "AzureStorageBusPreSessionConnectionString")]
            ServiceBusReceivedMessage message,
            ServiceBusMessageActions messageActions)
        {
            try
            {
                // Parse the event data
                var blobEvent = JsonSerializer.Deserialize<BlobCreatedEvent>(message.Body);

                if (blobEvent == null)
                {
                    // If it fails to parse, the message may unprocessable
                    await messageActions.DeadLetterMessageAsync(message);
                    throw new ArgumentException($"Invalid message body {message.MessageId} : {message.Body}");
                }

                logger.LogInformation("Message ID: {id}", message.MessageId);
                logger.LogInformation("Message File: {body}", blobEvent.data.url);

                await blobCreatedEventService.RequeueBlobCreatedWithSessionAndProperties(blobEvent, message);
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
