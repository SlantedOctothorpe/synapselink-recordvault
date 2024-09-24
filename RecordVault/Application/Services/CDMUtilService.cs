using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class CDMUtilService : ICDMService
    {
        public SqlCdmTable GetCDMEntityFromStorageAccountURL(AzureStorageURL storageURL)
        {
            return new SqlCdmTable(storageURL.GetBlobParentFolderName());
        }
    }
}
