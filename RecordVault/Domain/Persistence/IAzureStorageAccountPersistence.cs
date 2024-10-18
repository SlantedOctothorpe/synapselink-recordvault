using RecordVault.Domain.ValueObjects;

namespace RecordVault.Domain.Persistence
{
    public interface IAzureStorageAccountPersistence
    {
        public Task<StreamReader> GetStreamReaderFromURL(AzureStorageURL storageURL);
    }
}
