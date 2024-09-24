using Microsoft.Data.SqlClient;

using Sylvan.Data.Csv;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.Persistence
{
    public interface ISQLPersistence
    {
        public SqlConnection GetSQLConnection(string connectionString = "");

        public IEnumerable<DbColumn> GetSQLTableSchema(string tableName, SqlConnection sqlConnection);

        public void InsertCsvData(string tableName, SqlConnection sqlConnection, CsvDataReader csv);
    }
}
