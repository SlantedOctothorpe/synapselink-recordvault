using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Persistence
{
    public interface IQueuePersistence
    {
        Task EnqueueAsync(string queueName, string message, string sessionId = "");

        Task EnqueueComplexMessageAsync<T>(string queueName, T message);

        Task<T> CreateMessageReceiverForSessionAsync<T>(string queueName, string sessionId);

        Task CloseMessageReceiverAsync<T>(T receiver);

        Task<U> ReceiveMessageAsync<T, U>(T receiver, TimeSpan? maxWaitTime = null);

        Task CompleteMessageAsync<T, U>(T receiver, U message);

        Task DeadLetterMessageAsync<T, U>(T receiver, U message);
    }
}
