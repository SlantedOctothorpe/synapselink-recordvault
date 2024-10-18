using Microsoft.CommonDataModel.ObjectModel.Cdm;
using Microsoft.CommonDataModel.ObjectModel.Storage;
using Microsoft.CommonDataModel.ObjectModel.Utilities.Network;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using RecordVault.Domain.Services;
using RecordVault.Domain.ValueObjects;
using RecordVault.Infrastructure.Persistence;

namespace RecordVault.Application.Services
{
    public class CDMService : ICDMService
    {
        private readonly ILogger<CDMService> _logger;

        public CDMService(ILogger<CDMService> logger)
        {
            _logger = logger;
        }

        public string GetCDMEntityNameFromStorageAccountURL(AzureStorageURL storageURL)
        {
            return storageURL.GetBlobParentFolderName();
        }

        public List<string> GetCDMEntityList(string manifestURL = "")
        {
            throw new NotImplementedException();
        }

        public async Task<IEnumerable<SqlCdmTable>> GetCDMEntityMetadata(string manifestURL = "", string singleEntityName = "")
        {
            // TODO First attempt using hard coded values
            // May need to use a TokenProviderFactory to get the token provider

            var sqlMetadata = new List<SqlCdmTable>();

            var storageURL = new AzureStorageURL(manifestURL);

            var cdmCorpus = MountCdmCorpusStorage(storageURL);

            var manifestPath = $"{storageURL.BlobName}";

            CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>(manifestPath);

            // TODO Sub manifests

            if (!singleEntityName.IsNullOrEmpty())
            {
                var entityDeclaration = manifest.Entities.FirstOrDefault(e => e.EntityName == singleEntityName);
                if (entityDeclaration == null)
                {
                    _logger.LogError($"Entity {singleEntityName} not found in the manifest.");
                    throw new ArgumentException($"Entity {singleEntityName} not found in the manifest.");
                }

                var entity = await cdmCorpus.FetchObjectAsync<CdmEntityDefinition>(entityDeclaration.EntityPath, manifest);
                var sqlCdmTable = ProcessEntityDefinitionToSQLMetadata(entity);
                sqlMetadata.Add(sqlCdmTable);

                return sqlMetadata;
            }

            foreach (var entityListing in manifest.Entities)
            {
                var entity = await cdmCorpus.FetchObjectAsync<CdmEntityDefinition>(entityListing.EntityPath, manifest);
                var sqlCdmTable = ProcessEntityDefinitionToSQLMetadata(entity);
                sqlMetadata.Add(sqlCdmTable);
            }

            return sqlMetadata;
        }

        private CdmCorpusDefinition MountCdmCorpusStorage(AzureStorageURL manifestStorageURL)
        {
            var hostName = manifestStorageURL.GetDFSURL();
            //var hostName = storageURL.StorageAccountUri().ToString();
            if (hostName.EndsWith("/"))
            {
                hostName = hostName.Remove(hostName.Length - 1);
            }

            var rootFolder = manifestStorageURL.Container;
            if (rootFolder.EndsWith("/"))
            {
                rootFolder = rootFolder.Remove(rootFolder.Length - 1);
            }

            var tenantId = Environment.GetEnvironmentVariable("AzureTenantId") ?? "";
            TokenProvider msitokenProvider = new AzureTokenProvider(tenantId, hostName);

            var tmpSharedKey = Environment.GetEnvironmentVariable("AzureStorageAccountAccessKey") ?? "";

            var cdmCorpus = new CdmCorpusDefinition();

            // TODO msitoken still not working
            //cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
            //  hostName, // Hostname.
            //  rootFolder,
            //  msitokenProvider // Token provider.
            //));

            cdmCorpus.Storage.Mount("adls", new ADLSAdapter(
              hostName, // Hostname.
              rootFolder,
              tmpSharedKey
            ));

            cdmCorpus.Storage.DefaultNamespace = "adls";


            // Test Storage Adapter
            //var pathToManifestFolder = "./";

            //cdmCorpus.Storage.Mount("local", new LocalAdapter(pathToManifestFolder));

            //cdmCorpus.Storage.DefaultNamespace = "local";

            return cdmCorpus;
        }

        private SqlCdmTable ProcessEntityDefinitionToSQLMetadata(CdmEntityDefinition entity)
        {
            // TODO: Do I need to handle has.sqlViewDefinition? REF: CDMUtil - ManifestHandler.cs : 340

            var sqlCdmColumns = new List<SQLCdmColumn>();

            // Loop through the attributes and create SQLCdmColumn objects
            foreach (var attributeItem in entity.Attributes)
            {
                var cdmAttribute = (CdmTypeAttributeDefinition)attributeItem;

                var columnName = cdmAttribute.Name;
                var columnType = GetCdmAttributeDataType(cdmAttribute);
                var columnNullable = cdmAttribute.IsNullable;
                var columnMaxLength = GetCdmAttributeMaxLength(cdmAttribute);
                var columnPrecision = GetCdmAttributeDecimalPrecision(cdmAttribute);
                var columnScale = GetCdmAttributeDecimalScale(cdmAttribute);

                var sqlCdmColumn = new SQLCdmColumn(
                    columnName,
                    columnType,
                    columnNullable,
                    columnMaxLength,
                    columnPrecision,
                    columnScale
                );
                sqlCdmColumns.Add(sqlCdmColumn);
            }

            var sqlCdmTable = new SqlCdmTable(entity.EntityName, sqlCdmColumns);

            return sqlCdmTable;
        }

        private string GetCdmAttributeDataType(CdmTypeAttributeDefinition cdmAttribute)
        {
            // TODO custom data types e.g. SYSROWVERSION to be timestamp or rowversion not int64
            var dataType = cdmAttribute.DataFormat.ToString();
            if (cdmAttribute.DataType != null)
            {
                dataType = cdmAttribute.DataType.NamedReference;
            }

            return dataType;
        }

        private int GetCdmAttributeMaxLength(CdmTypeAttributeDefinition cdmAttribute)
        {
            var maxLength = -1;
            if (cdmAttribute.MaximumLength != null)
            {
                maxLength = (int)cdmAttribute.MaximumLength;
            }
            else if (cdmAttribute.AppliedTraits != null)
            {
                CdmTraitReference? trait = cdmAttribute.AppliedTraits.FirstOrDefault(x => x.NamedReference == "is.constrained") as CdmTraitReference;

                if (trait != null)
                {
                    var argument = trait.Arguments.FirstOrDefault(x => x.Name == "maximumLength");
                    
                    if (argument != null)
                    {
                        maxLength = (int)argument.Value;
                    }
                }
            }

            // TODO Do we need default values?

            return maxLength;
        }

        private int GetCdmAttributeDecimalPrecision(CdmTypeAttributeDefinition cdmAttribute)
        {
            var precision = 38;

            if (cdmAttribute.AppliedTraits != null)
            {
                CdmTraitReference? trait = cdmAttribute.AppliedTraits.FirstOrDefault(x => x.NamedReference == "is.dataFormat.numeric.shaped") as CdmTraitReference;

                if (trait != null)
                {
                    var argument = trait.Arguments.FirstOrDefault(x => x.Name == "precision");

                    if (argument != null)
                    {
                        precision = (int)argument.Value;
                    }
                }
            }

            return precision;
        }

        private int GetCdmAttributeDecimalScale(CdmTypeAttributeDefinition cdmAttribute)
        {
            var scale = 6;

            if (cdmAttribute.AppliedTraits != null)
            {
                CdmTraitReference? trait = cdmAttribute.AppliedTraits.FirstOrDefault(x => x.NamedReference == "is.dataFormat.numeric.shaped") as CdmTraitReference;

                if (trait != null)
                {
                    var argument = trait.Arguments.FirstOrDefault(x => x.Name == "scale");

                    if (argument != null)
                    {
                        scale = (int)argument.Value;
                    }
                }
            }

            return scale;
        }

        // TODO Remove this method
        private async Task tempLocalCDM()
        {
            var cdmCorpus = new CdmCorpusDefinition();

            var pathToManifestFolder = "./";

            cdmCorpus.Storage.Mount("local", new LocalAdapter(pathToManifestFolder));

            cdmCorpus.Storage.DefaultNamespace = "local";

            CdmManifestDefinition manifest = await cdmCorpus.FetchObjectAsync<CdmManifestDefinition>("model.json");

            _logger.LogInformation("Sub-manifests in the model.json:" + manifest.SubManifests.Count);

            if (manifest.Entities.Count > 0)
            {
                _logger.LogInformation("List of all entities:");

                foreach (var entDec in manifest.Entities)
                {
                    // Print entity declarations.
                    // Assume there are only local entities in this manifest for simplicity.
                    _logger.LogInformation("  " + entDec.EntityName.PadRight(35) + "  " + entDec.EntityPath);
                }
            }

            _logger.LogInformation("");

            foreach (var entityListing in manifest.Entities)
            {
                var entity = await cdmCorpus.FetchObjectAsync<CdmEntityDefinition>(entityListing.EntityPath, manifest);

                // Print the properties of the entity.
                _logger.LogInformation("Diplaying Entity: " + entity.EntityName);

                // Attributes
                // Implicit cast
                _logger.LogInformation("  Attributes: " + entity.Attributes.Count);
                foreach (CdmTypeAttributeDefinition attribute in entity.Attributes)
                {
                    _logger.LogInformation("    Name: " + attribute.Name);
                    _logger.LogInformation("    DataFormat: " + attribute.DataFormat.ToString());
                    _logger.LogInformation("    AppliedTraits: Ignored for now");
                }

                _logger.LogInformation("");

                // Traits
                // TODO: Trait name - has.sqlViewDefiniton?
                _logger.LogInformation("  Traits: " + entity.ExhibitsTraits.Count);
                _logger.LogInformation("    Ignored for now");

                _logger.LogInformation("");

                // Properties
                _logger.LogInformation("  Properties:");
                _logger.LogInformation("    EntityName: " + entity.EntityName);
                if (entity.ExtendsEntity != null) _logger.LogInformation("    ExtendsEntity: " + entity.ExtendsEntity.FetchObjectDefinitionName());
                _logger.LogInformation("    DisplayName: " + entity.DisplayName);
                _logger.LogInformation("    Description: " + entity.Description);
                _logger.LogInformation("    Version: " + entity.Version);
                _logger.LogInformation("    SourceName: " + entity.SourceName);
                _logger.LogInformation("    LastFileModifiedTime: " + entityListing.LastFileModifiedTime);
                _logger.LogInformation("    LastFileStatusCheckTime: " + entityListing.LastFileStatusCheckTime);
                if (entity.CdmSchemas != null)
                {
                    _logger.LogInformation("    CdmSchemas: " + entity.CdmSchemas.Count);
                    foreach (var schema in entity.CdmSchemas)
                    {
                        _logger.LogInformation("      SchemaName: " + schema);
                    }
                }

                _logger.LogInformation("");

                // Partition Locations
                _logger.LogInformation("  Partition Locations: " + entityListing.DataPartitions.Count);
                foreach (var dataPartition in entityListing.DataPartitions)
                {
                    _logger.LogInformation("    Location: " + dataPartition.Location);
                    if (!string.IsNullOrEmpty(dataPartition.Location))
                    {
                        _logger.LogInformation("      StoragePath: " + cdmCorpus.Storage.CorpusPathToAdapterPath(dataPartition.Location));
                    }
                }

                _logger.LogInformation("");

                // Relationships
                if (manifest.Relationships != null && manifest.Relationships.Count > 0)
                {
                    _logger.LogInformation("  Relationships: " + manifest.Relationships.Count);
                    foreach (var relationship in manifest.Relationships)
                    {
                        // Currently, the easiest way to get a specific entity's relationships (given a resolved manifest) is
                        // to just look at all the entity relationships in the resolved manifest, and then filtering.

                        if (relationship.FromEntity.Contains(entity.EntityName) || relationship.ToEntity.Contains(entity.EntityName))
                        {
                            _logger.LogInformation($"    FromEntity: {relationship.FromEntity}");
                            _logger.LogInformation($"    FromEntityAttribute: {relationship.FromEntityAttribute}");
                            _logger.LogInformation($"    ToEntity: {relationship.ToEntity}");
                            _logger.LogInformation($"    ToEntityAttribute: {relationship.ToEntityAttribute}");
                            _logger.LogInformation("");

                        }
                    }
                } else
                {
                    // The manifest file doesn't contain relationships, so we have to compute the relationships first.
                    await cdmCorpus.CalculateEntityGraphAsync(manifest);

                    _logger.LogInformation("  Incoming Relationships:");
                    foreach (var relationship in cdmCorpus.FetchIncomingRelationships(entity))
                    {
                        _logger.LogInformation($"    FromEntity: {relationship.FromEntity}");
                        _logger.LogInformation($"    FromEntityAttribute: {relationship.FromEntityAttribute}");
                        _logger.LogInformation($"    ToEntity: {relationship.ToEntity}");
                        _logger.LogInformation($"    ToEntityAttribute: {relationship.ToEntityAttribute}");
                        _logger.LogInformation("");
                    }

                    _logger.LogInformation("  Outgoing Relationships:");
                    foreach (var relationship in cdmCorpus.FetchOutgoingRelationships(entity))
                    {
                        _logger.LogInformation($"    FromEntity: {relationship.FromEntity}");
                        _logger.LogInformation($"    FromEntityAttribute: {relationship.FromEntityAttribute}");
                        _logger.LogInformation($"    ToEntity: {relationship.ToEntity}");
                        _logger.LogInformation($"    ToEntityAttribute: {relationship.ToEntityAttribute}");
                        _logger.LogInformation("");
                    }
                }

                _logger.LogInformation("");
            }

            return;
        }
    }
}
