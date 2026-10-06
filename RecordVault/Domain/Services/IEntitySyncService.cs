using RecordVault.Domain.ValueObjects;
using RecordVault.DTOs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Services
{
    public interface IEntitySyncService
    {
        public IEnumerable<EntitySyncPackage> BlobCreatedEventsToSyncPackages(IEnumerable<BlobCreatedEvent> blobCreatedEvents);

        public Task<IEnumerable<SqlCdmTable>> SyncEntityCDMSchema(IEnumerable<EntitySyncPackage> entitySyncPackages);

        public Task<IEnumerable<SqlCdmTable>> SyncEntityCDMSchema(IEnumerable<BlobCreatedEvent> blobCreatedEvents);

        public Task<int> SyncEntityData(IEnumerable<EntitySyncPackage> entitySyncPackages, IEnumerable<SqlCdmTable>? sqlCdmTables = null);

        public Task<int> SyncEntityData(IEnumerable<BlobCreatedEvent> blobCreatedEvents, IEnumerable<SqlCdmTable>? sqlCdmTables = null);
    }
}
