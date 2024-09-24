using RecordVault.Domain.ValueObjects;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Persistence
{
    public interface IAzureStorageAccountPersistence
    {
        public Task<StreamReader> GetStreamReaderFromURL(AzureStorageURL storageURL);
    }
}
