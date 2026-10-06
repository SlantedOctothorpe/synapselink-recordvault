using Azure.Messaging.ServiceBus;

using Microsoft.Azure.Functions.Worker;

using RecordVault.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Services
{
    public interface IBlobCreatedEventService
    {
        Task RequeueBlobCreatedWithSession(BlobCreatedEvent blobCreatedEvent);

        Task RequeueBlobCreatedWithSessionAndProperties<T>(BlobCreatedEvent blobCreatedEvent, T originalMessage);

        Task<(IEnumerable<BlobCreatedEvent> events, IEnumerable<ServiceBusReceivedMessage> messages)> SBMessagesToDeDedupedBlobCreatedEventsAsync(ServiceBusReceivedMessage[] blobCreatedMessages, ServiceBusMessageActions messageActions);
    }
}
