using RecordVault.Domain.ValueObjects;

namespace RecordVault.Domain.Services
{
    public interface ICDMService
    {
        public string GetCDMEntityNameFromStorageAccountURL(AzureStorageURL storageURL);

        public Task<IEnumerable<SqlCdmTable>> GetCDMEntityMetadata (string manifestURL = "", string singleEntityName = "");
    }
}
