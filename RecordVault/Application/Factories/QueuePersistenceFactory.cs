using RecordVault.Domain.Persistence;
using RecordVault.Infrastructure.Persistence;

using Microsoft.Extensions.DependencyInjection;

namespace RecordVault.Application.Factories
{
    public class QueuePersistenceFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public QueuePersistenceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IQueuePersistence GetQueuePersistence(string queueType = "servicebus")
        {
            return queueType.ToLower() switch
            {
                "servicebux" => _serviceProvider.GetRequiredService<ServiceBusQueuePersistence>(),
                _ => throw new ArgumentException($"Unsupported queue type: {queueType}", nameof(queueType))
            };
        }
    }
}
