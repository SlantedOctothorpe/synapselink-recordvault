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

        /// <summary>
        /// Create a new table with the same columns as the current table, but with a name including the staging prefix
        /// </summary>
        /// <remarks>
        /// This is a shallow copy of columns only, changes to the new or existing object will update both
        /// </remarks>
        /// <returns>A copy of <c>SqlCdmTable</c></returns>
        /// <exception cref="ArgumentException"></exception>
        public SqlCdmTable CopyAsStagingTable()
        {
            var stagingPrefix = Environment.GetEnvironmentVariable("RecordVaultDBStagingTablePrefix");
            if (string.IsNullOrEmpty(stagingPrefix))
            {
                throw new ArgumentException("Staging Table Prefix not provided");
            }

            // This is a shallow copy only, the columns are not cloned
            var newTableName = $"{stagingPrefix}{TableName}";
            var stagingTable = new SqlCdmTable(newTableName, Columns);

            return stagingTable;
        }
    }
}
