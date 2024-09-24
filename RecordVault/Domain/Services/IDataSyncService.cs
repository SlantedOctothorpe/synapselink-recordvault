namespace RecordVault.Domain.Services
{
    public interface IDataSyncService
    {
        public Task SyncStorageAccountFile(string fileURL);
    }
}
