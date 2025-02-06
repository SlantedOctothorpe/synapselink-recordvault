using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using RecordVault.Application.Factories;

namespace RecordVault.Functions
{
    public class SessionSpawnTimer(ILogger<SessionSpawnTimer> logger, QueueServiceFactory queueServiceFactory)
    {
        [Function(nameof(SessionSpawnTimer))]
        //public void Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer)
        public void Run([TimerTrigger("*/20 * * * * *")] TimerInfo myTimer)
        {
            logger.LogInformation("Starting Session Processing instances");

            var queueService = queueServiceFactory.GetQueueService("servicebus");
            var triggerProcessQueue = Environment.GetEnvironmentVariable("AzureStorageBusTriggerProcessQueueName") ?? "rds-trigger-processing";

            var processInstances = int.TryParse(Environment.GetEnvironmentVariable("ProcessBlobCreatedInstances") ?? "5", out var instances) ? instances : 5;
            for (var i = 0; i < processInstances; i++)
            {
                queueService.EnqueueAsync(triggerProcessQueue, "ProcessNextBlobCreatedSession");
            }

            logger.LogInformation("Finished Session Processing instances");
        }

    }
}
