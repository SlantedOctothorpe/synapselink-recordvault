using Microsoft.Data.SqlClient;

using Sylvan.Data.Csv;
using System.Data;
using System.Data.Common;

namespace RecordVault.Domain.Persistence
{
    public interface ISQLPersistence
    {
        public SqlConnection GetSQLConnection(string connectionString = "");

        public bool CheckTableExists(string tableName, SqlConnection sqlConnection);

        /// <summary>
        /// Fetches schema for a SQL table using the passed connection
        /// </summary>
        /// <remarks>
        /// The column details for the schema are at https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqldatareader.getschematable?view=netframework-4.8.1&viewFallbackFrom=net-8.0
        /// </remarks>
        /// <param name="tableName"></param>
        /// <param name="sqlConnection"></param>
        /// <returns>Sytem.Data.DataTable with fields with column information</returns>
        public DataTable GetSQLTableSchema(string tableName, SqlConnection sqlConnection);

        public IEnumerable<DbColumn> GetSQLColumnSchema(string tableName, SqlConnection sqlConnection);

        public void InsertCsvData(string tableName, SqlConnection sqlConnection, CsvDataReader csv);

        public void ExecuteNonQuery(string query, SqlConnection sqlConnection);

        Task InsertCsvDataAsync(string tableName, SqlConnection sqlConnection, List<object[]> batch);
    }
}
