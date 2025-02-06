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
        Task EnqueueAsync(string queueName, string message, string sessionId = "");

        Task EnqueueComplexMessageAsync<T>(string queueName, T message);

        Task<T> GetMessageReceiverForSessionAsync<T>(string queueName, string sessionId);

        Task<T> GetMessageReceiverForNextSessionAsync<T>(string queueName);

        Task CloseMessageReceiverAsync<T>(T receiver);

        Task<IEnumerable<U>> GetDedupedBlobCreatedSessionMessagesAsync<T, U>(T receiver);

        Task CompleteMessageAsync<T, U>(T receiver, U message);

        Task DeadLetterMessageAsync<T, U>(T receiver, U message);
    }
}
