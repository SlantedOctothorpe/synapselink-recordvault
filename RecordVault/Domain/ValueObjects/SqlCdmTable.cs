using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.ValueObjects
{
    public class SqlCdmTable
    {
        public string TableName { get; private set; }

        public IEnumerable<SQLCdmColumn> Columns { get; private set; }

        public SqlCdmTable(string tableName, IEnumerable<SQLCdmColumn> columns)
        {
            TableName = tableName;
            Columns = columns;
        }

        public string GetTableStagingName()
        {
            var stagingPrefix = Environment.GetEnvironmentVariable("RecordVaultDBStagingTablePrefix");
            if (string.IsNullOrEmpty(stagingPrefix))
            {
                throw new ArgumentException("Staging Table Prefix not provided");
            }

            return $"{stagingPrefix}{TableName}";
        }
    }
}
