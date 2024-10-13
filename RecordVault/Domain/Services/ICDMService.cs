using RecordVault.Domain.ValueObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Services
{
    public interface ICDMService
    {
        public string GetCDMEntityNameFromStorageAccountURL(AzureStorageURL storageURL);

        public List<string> GetCDMEntityList (string manifestURL = "");

        public Task<IEnumerable<SqlCdmTable>> GetCDMEntityMetadata (string manifestURL = "", string singleEntityName = "");
    }
}
