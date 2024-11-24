using RecordVault.Domain.ValueObjects;

namespace RecordVault.Domain.Persistence
{
    public interface IAzureStorageAccountPersistence
    {
        Task<Stream> GetStreamFromURL(AzureStorageURL storageURL);
    }
}
