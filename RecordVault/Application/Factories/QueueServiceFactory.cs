using Microsoft.Extensions.DependencyInjection;

using RecordVault.Application.Services;
using RecordVault.Domain.Services;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Application.Factories
{
    public class QueueServiceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QueueServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQueueService GetQueuePersistence(string queueType = "servicebus")
        {
            return queueType.ToLower() switch
            {
                "servicebux" => _serviceProvider.GetRequiredService<ServiceBusQueueService>(),
                _ => throw new ArgumentException($"Unsupported queue type: {queueType}", nameof(queueType))
            };
        }
    }
}
