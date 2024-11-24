using RecordVault.Domain.ValueObjects;

namespace RecordVault.Domain.Services
{
    public interface ISQLSchemaManagementService
    {
        public void CreateOrUpdateTable(SqlCdmTable table);

        public string GenerateMergeCode(SqlCdmTable baseTable, SqlCdmTable stagingTable);
    }
}
