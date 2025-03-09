namespace RecordVault.Domain.Services
{
    public interface IDataSyncService
    {
        public Task<int> SyncStorageAccountFile(string fileURL);
    }
}
