using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using RecordVault.Domain.Persistence;
using System.Data;
using System.Data.Common;

namespace RecordVault.Infrastructure.Persistence
{
    public class SQLServerPersistence : ISQLPersistence
    {
        public IDbConnection GetSQLConnection(string connectionString = "")
        {
            return GetSqlServerConnection(connectionString);
        }

        public SqlConnection GetSqlServerConnection(string connectionString = "")
        {
            if (connectionString.IsNullOrEmpty())
            {
                connectionString = Environment.GetEnvironmentVariable("RecordVaultDBConnectionString") ?? "";
            }

            if (connectionString.IsNullOrEmpty())
            {
                throw new ArgumentException("SQL Connection String not provided");
            }

            return new SqlConnection(connectionString);
        }

        public bool CheckTableExists(string tableName, IDbConnection connection)
        {
            var sqlConnection = (SqlConnection)connection;
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"select case when exists((select * from information_schema.tables where table_name = '{tableName}')) then 1 else 0 end";
            command.CommandType = CommandType.Text;

            sqlConnection.Open();
            var count = (int)command.ExecuteScalar();
            sqlConnection.Close();

            return count > 0;
        }

        public DataTable GetSQLTableSchema(string tableName, IDbConnection connection)
        {
            var sqlConnection = (SqlConnection)connection;
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT top 0 * FROM {tableName}";
            command.CommandType = CommandType.Text;

            sqlConnection.Open();
            var reader = command.ExecuteReader(CommandBehavior.KeyInfo);
            var tableSchema = reader.GetSchemaTable();
            sqlConnection.Close();

            return tableSchema;
        }

        public IEnumerable<DbColumn> GetSQLColumnSchema(string tableName, IDbConnection connection)
        {
            var sqlConnection = (SqlConnection)connection;
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT top 0 * FROM {tableName}";

            sqlConnection.Open();
            var reader = command.ExecuteReader();
            var tableSchema = reader.GetColumnSchema();
            sqlConnection.Close();

            return tableSchema;
        }

        public int GetRowCount(string tableName, IDbConnection connection)
        {
            var sqlConnection = (SqlConnection)connection;
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM {tableName} WITH (NOLOCK)";
            command.CommandType = CommandType.Text;

            sqlConnection.Open();
            var count = (int)(command.ExecuteScalar() ?? 0);
            sqlConnection.Close();

            return count;
        }

        public void ExecuteNonQuery(string query, IDbConnection connection)
        {
            var sqlConnection = (SqlConnection)connection;
            var command = sqlConnection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 0;

            sqlConnection.Open();
            command.ExecuteNonQuery();
            sqlConnection.Close();
        }

        public void TruncateTable(string tableName, IDbConnection connection)
        {
            var truncateScript = $"TRUNCATE TABLE {tableName}";
            ExecuteNonQuery(truncateScript, connection);
        }

        public async Task InsertDataAsync(string tableName, IDbConnection connection, IDataReader dataReader)
        {
            var sqlConnection = (SqlConnection)connection;
            using var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = tableName,
                BulkCopyTimeout = 0,
                BatchSize = 10000
            };

            try
            {
                await sqlConnection.OpenAsync();
                await bulkCopy.WriteToServerAsync(dataReader);
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    await sqlConnection.CloseAsync();
                }
            }
        }

        public async Task InsertDataAsync(string tableName, IDbConnection connection, List<object[]> batch)
        {
            await InsertCsvDataAsync(tableName, (SqlConnection)connection, batch);
        }

        public async Task InsertCsvDataAsync(string tableName, SqlConnection sqlConnection, List<object[]> batch)
        {
            var dataTable = CreateDataTable(tableName, sqlConnection, batch);

            using var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = tableName,
                BulkCopyTimeout = 0,
                BatchSize = 10000
            };

            try
            {
                await sqlConnection.OpenAsync();
                await bulkCopy.WriteToServerAsync(dataTable);
            }
            finally
            {
                if (sqlConnection.State == ConnectionState.Open)
                {
                    await sqlConnection.CloseAsync();
                }
            }
        }

        private DataTable CreateDataTable(string tableName, SqlConnection sqlConnection, List<object[]> batch)
        {
            var schema = GetSQLColumnSchema(tableName, sqlConnection);
            var dataTable = new DataTable();

            foreach (var column in schema)
            {
                dataTable.Columns.Add(column.ColumnName, column.DataType ?? typeof(object));
            }

            foreach (var row in batch)
            {
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }
    }
}