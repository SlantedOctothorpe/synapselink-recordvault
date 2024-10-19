using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace RecordVault.Domain.Persistence
{
    public interface ISQLPersistence
    {
        IDbConnection GetSQLConnection(string connectionString = "");
        bool CheckTableExists(string tableName, IDbConnection connection);

        /// <summary>
        /// Fetches schema for a SQL table using the passed connection
        /// </summary>
        /// <remarks>
        /// The column details for the schema are at https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqldatareader.getschematable?view=netframework-4.8.1&viewFallbackFrom=net-8.0
        /// </remarks>
        /// <param name="tableName"></param>
        /// <param name="connection"></param>
        /// <returns>System.Data.DataTable with fields with column information</returns>
        DataTable GetSQLTableSchema(string tableName, IDbConnection connection);

        IEnumerable<DbColumn> GetSQLColumnSchema(string tableName, IDbConnection connection);

        void ExecuteNonQuery(string query, IDbConnection connection);

        Task InsertDataAsync(string tableName, IDbConnection connection, IDataReader dataReader);
        Task InsertDataAsync(string tableName, IDbConnection connection, List<object[]> batch);

        SqlConnection GetSqlServerConnection(string connectionString = "");
        Task InsertCsvDataAsync(string tableName, SqlConnection sqlConnection, List<object[]> batch);
    }
}