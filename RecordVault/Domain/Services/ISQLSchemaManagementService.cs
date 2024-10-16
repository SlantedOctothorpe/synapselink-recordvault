using RecordVault.Domain.ValueObjects;

namespace RecordVault.Domain.Services
{
    public interface ISQLSchemaManagementService
    {
        public void CreateOrUpdateTable(SqlCdmTable table);

        public void CreateOrUpdateMergeCode(SqlCdmTable originalTable, SqlCdmTable stagingTable);
    }
}
