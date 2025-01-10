using Azure.Messaging.ServiceBus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Services
{
    public interface IQueueService
    {
        Task<T> GetMessageReceiverForSession<T>(string queueName, string sessionId);

        Task CloseMessageReceivier<T>(T receiver);

        Task<IEnumerable<U>> GetDedupedBlobCreatedSessionMessages<T, U>(T receiver);

        Task CompleteMessage<T, U>(T receiver, U message);

        Task DeadLetterMessage<T, U>(T receiver, U message);
    }
}
