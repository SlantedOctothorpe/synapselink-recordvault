using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;
using RecordVault.DTOs;

namespace RecordVault.Application.Services
{
    public class EntitySyncService(
        ILogger<EntitySyncService> logger,
        ICDMService cdmService,
        SQLSchemaManagementFactory sqlSchemaManagementFactory,
        IDataSyncService dataSyncService,
        SQLPersistenceFactory sqlPersistenceFactory
        )
    : IEntitySyncService
    {
        public IEnumerable<EntitySyncPackage> BlobCreatedEventsToSyncPackages(IEnumerable<BlobCreatedEvent> blobCreatedEvents)
        {
            var entitySyncPackages = new List<EntitySyncPackage>();

            foreach (var blobCreatedEvent in blobCreatedEvents)
            {
                var fileURL = blobCreatedEvent.data.blobUrl;
                var storageURL = new AzureStorageURL(fileURL);

                var entityName = cdmService.GetCDMEntityNameFromStorageAccountURL(storageURL);
                var entitySyncPackage = entitySyncPackages.FirstOrDefault(x => x.EntityName == entityName);

                if (entitySyncPackage == null)
                {
                    entitySyncPackage = new EntitySyncPackage
                    {
                        EntityName = entityName,
                        EntityFileURLs = new List<string>()
                    };

                    entitySyncPackages.Add(entitySyncPackage);
                }

                if (!entitySyncPackage.EntityFileURLs.Contains(fileURL)) entitySyncPackage.EntityFileURLs.Add(fileURL);
            }

            return entitySyncPackages;
        }

        public async Task<IEnumerable<SqlCdmTable>> SyncEntityCDMSchema(IEnumerable<EntitySyncPackage> entitySyncPackages)
        {
            var metadataURL = Environment.GetEnvironmentVariable("CDMManifestURL") ?? "";
            var sqlMetadata = await cdmService.GetCDMEntityMetadata(metadataURL);

            var sqlSchemaManagementService = sqlSchemaManagementFactory.GetSQLSchemaManagementService();

            var tablesSynced = new List<SqlCdmTable>();

            foreach (var entitySyncPackage in entitySyncPackages)
            {
                var entityName = entitySyncPackage.EntityName;

                logger.LogInformation($"Syncing schema for entity {entityName}");

                var entityTable = sqlMetadata.FirstOrDefault(x => x.TableName == entityName)
                    ?? throw new Exception($"Entity {entityName} not found in CDM metadata");

                var entityStagingTable = entityTable.CopyAsStagingTable();

                sqlSchemaManagementService.CreateOrUpdateTable(entityTable);

                sqlSchemaManagementService.CreateOrUpdateTable(entityStagingTable);

                tablesSynced.Add(entityTable);
            }

            return tablesSynced;
        }

        public async Task<IEnumerable<SqlCdmTable>> SyncEntityCDMSchema(IEnumerable<BlobCreatedEvent> blobCreatedEvents)
        {
            var entitySyncPackages = BlobCreatedEventsToSyncPackages(blobCreatedEvents);
            return await SyncEntityCDMSchema(entitySyncPackages);
        }

        public async Task<int> SyncEntityData(IEnumerable<EntitySyncPackage> entitySyncPackages, IEnumerable<SqlCdmTable>? sqlCdmTables = null)
        {
            var tablesToSync = sqlCdmTables;
            if (tablesToSync == null)
            {
                var metadataURL = Environment.GetEnvironmentVariable("CDMManifestURL") ?? "";
                var sqlMetadata = await cdmService.GetCDMEntityMetadata(metadataURL);

                // TODO this seems very inefficient
                tablesToSync = sqlMetadata.Where(t => entitySyncPackages.Any(e => e.EntityName.Equals(t.TableName)));

                if (tablesToSync == null)
                {
                    throw new ArgumentException("No metadata found for syncing entities");
                }

                if (tablesToSync.Count() != entitySyncPackages.Count())
                {
                    throw new ArgumentException("Not all entities have corresponding metadata");
                }
            }

            var sqlSchemaManagementService = sqlSchemaManagementFactory.GetSQLSchemaManagementService();
            var sqlPersistence = sqlPersistenceFactory.GetSQLPersistence();
            var sqlConnection = sqlPersistence.GetSQLConnection();

            var totalRowCount = 0;
            foreach (var entitySyncPackage in entitySyncPackages)
            {
                logger.LogInformation($"Syncing data for entity {entitySyncPackage.EntityName}");

                var entityTable = tablesToSync.FirstOrDefault(t => t.TableName == entitySyncPackage.EntityName)
                    ?? throw new Exception($"Entity {entitySyncPackage.EntityName} not found in CDM metadata");

                var entityStagingTable = entityTable.CopyAsStagingTable();

                sqlPersistence.TruncateTable(entityStagingTable.TableName, sqlConnection);

                foreach (var fileURL in entitySyncPackage.EntityFileURLs)
                {
                    logger.LogInformation($"Syncing data for {entitySyncPackage.EntityName} from {fileURL}");

                    var insertRowCount = await dataSyncService.SyncStorageAccountFile(fileURL);

                    totalRowCount += insertRowCount;
                }

                var mergeScript = sqlSchemaManagementService.GenerateMergeCode(entityTable, entityStagingTable);

                sqlPersistence.ExecuteNonQuery(mergeScript, sqlConnection);
            }

            return totalRowCount;
        }

        public async Task<int> SyncEntityData(IEnumerable<BlobCreatedEvent> blobCreatedEvents, IEnumerable<SqlCdmTable>? sqlCdmTables = null)
        {
            var entitySyncPackages = BlobCreatedEventsToSyncPackages(blobCreatedEvents);
            return await SyncEntityData(entitySyncPackages, sqlCdmTables);
        }
    }
}
