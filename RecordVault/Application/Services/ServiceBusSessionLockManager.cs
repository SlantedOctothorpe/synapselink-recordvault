using Azure.Messaging.ServiceBus;

using Microsoft.Extensions.Logging;

using RecordVault.Domain.Services;

namespace RecordVault.Application.Services
{
    public class ServiceBusSessionLockManager : IQueueSessionLockManager
    {
        private readonly ServiceBusSessionReceiver _receiver;
        private readonly TimeSpan _renewalBuffer = TimeSpan.FromSeconds(10);
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _renewalTask;
        private ILogger<ServiceBusSessionLockManager> _logger;

        public ServiceBusSessionLockManager(ServiceBusSessionReceiver receiver)
        {
            _receiver = receiver;

            var factory = LoggerFactory.Create(builder => {
                builder.AddConsole();
            });

            _logger = factory.CreateLogger<ServiceBusSessionLockManager>();
        }

        public void StartSessionLockRenewal()
        {
            _cancellationTokenSource = new CancellationTokenSource();
            _renewalTask = Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    var timeUntilExpiry = _receiver.SessionLockedUntil - DateTimeOffset.UtcNow;

                    if (timeUntilExpiry < _renewalBuffer)
                    {
                        var sessionId = _receiver.SessionId;

                        try
                        {
                            await _receiver.RenewSessionLockAsync(_cancellationTokenSource.Token);
                            _logger.LogInformation($"Session ({sessionId}) lock renewed at {DateTime.UtcNow}. New expiry: {_receiver.SessionLockedUntil}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"Failed to renew session ({sessionId} lock: {ex.Message}");
                        }
                    }

                    // Wait a bit before checking again
                    await Task.Delay(TimeSpan.FromSeconds(5), _cancellationTokenSource.Token);
                }
            });
        }

        public void StopSessionLockRenewal()
        {
            if (_cancellationTokenSource == null || _renewalTask == null) throw new InvalidOperationException("Session lock renewal not started");

            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }
    }
}
