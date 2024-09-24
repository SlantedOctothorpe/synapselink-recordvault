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
        public SqlCdmTable GetCDMEntityFromStorageAccountURL(AzureStorageURL storageURL);
    }
}
