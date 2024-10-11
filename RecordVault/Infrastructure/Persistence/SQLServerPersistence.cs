using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;

using RecordVault.Domain.Persistence;

using Sylvan.Data.Csv;

using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Infrastructure.Persistence
{
    public class SQLServerPersistence : ISQLPersistence
    {
        public SqlConnection GetSQLConnection(string connectionString = "")
        {
            if (connectionString.IsNullOrEmpty())
            {
                connectionString = Environment.GetEnvironmentVariable("RecodVaultDBConnectionString") ?? "";
            }

            if (connectionString.IsNullOrEmpty())
            {
                throw new ArgumentException("SQL Connection String not provided");
            }

            var conn = new SqlConnection(connectionString);
            return conn;
        }

        public bool CheckTableExists(string tableName, SqlConnection sqlConnection)
        {
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT COUNT(*) FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_NAME = '{tableName}'";
            command.CommandType = CommandType.Text;

            sqlConnection.Open();

            var count = (int)command.ExecuteScalar();

            sqlConnection.Close();

            return count > 0;
        }

        public DataTable GetSQLTableSchema(string tableName, SqlConnection sqlConnection)
        {
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT top 0 * FROM {tableName}";
            command.CommandType = CommandType.Text;

            sqlConnection.Open();

            // CommandBehavior.KeyInfo is required to get column schema
            // as per https://stackoverflow.com/questions/173834/getting-the-schema-for-a-table
            var reader = command.ExecuteReader(CommandBehavior.KeyInfo);
            var tableSchema = reader.GetSchemaTable();

            sqlConnection.Close();

            return tableSchema;
        }

        public IEnumerable<DbColumn> GetSQLColumnSchema(string tableName, SqlConnection sqlConnection)
        {
            // This method is a modified version of the example provided in the Sylvan.Data.Csv documentation:
            // https://github.com/MarkPflug/Sylvan/blob/main/docs/Csv/Examples.md#bulk-load-csv-data-into-sqlserver

            // TODO will table always be in dbo schema?
            var command = sqlConnection.CreateCommand();
            command.CommandText = $"SELECT top 0 * FROM {tableName}";
            var reader = command.ExecuteReader();
            var tableSchema = reader.GetColumnSchema();

            return tableSchema;
        }

        public void InsertCsvData(string tableName, SqlConnection sqlConnection, CsvDataReader csv)
        {
            // This method is a modified version of the example provided in the Sylvan.Data.Csv documentation:
            // https://github.com/MarkPflug/Sylvan/blob/main/docs/Csv/Examples.md#bulk-load-csv-data-into-sqlserver

            var bulkCopy = new SqlBulkCopy(sqlConnection)
            {
                DestinationTableName = tableName
            };
            bulkCopy.BulkCopyTimeout = 0;
            bulkCopy.BatchSize = 10000;
            bulkCopy.WriteToServer(csv);
        }

        public void ExecuteNonQuery(string query, SqlConnection sqlConnection)
        {
            // Runs query not expecting a result set
            var command = sqlConnection.CreateCommand();
            command.CommandText = query;
            command.CommandType = CommandType.Text;

            sqlConnection.Open();

            command.ExecuteNonQuery();

            sqlConnection.Close();
        }
    }
}
