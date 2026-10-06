namespace RecordVault.Domain.Services
{
    public interface IQueueSessionLockManager
    {
        void StartSessionLockRenewal();

        void StopSessionLockRenewal();
    }
}
