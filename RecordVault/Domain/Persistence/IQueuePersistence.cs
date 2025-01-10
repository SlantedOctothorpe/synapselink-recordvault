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
    }
}
