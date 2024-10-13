using Microsoft.Extensions.Logging;

using RecordVault.Application.Factories;
using RecordVault.Domain.Services;

using Sylvan.Data.Csv;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Application.Services
{
    public class SylvanCSVService : ICSVProcessingService
    {
        private readonly ILogger<SylvanCSVService> _logger;
        private readonly SQLPersistenceFactory _sqlPersistenceFactory;

        public SylvanCSVService(ILogger<SylvanCSVService> logger, SQLPersistenceFactory sqlPersistenceFactory)
        {
            _logger = logger;
            _sqlPersistenceFactory = sqlPersistenceFactory;
        }

        public void CSVStreamReaderToSQL(StreamReader streamReader, string tableName, string connectionString = "", string sqlType = "")
        {
            var sqlPersistence = _sqlPersistenceFactory.GetSQLPersistence(sqlType);

            // May need to add a check for the SQL type here #TODO
            var sqlConnection = sqlPersistence.GetSQLConnection(connectionString);
            var sqlTableSchema = sqlPersistence.GetSQLColumnSchema(tableName, sqlConnection);

            // What happens when/if sql columns are out of order from CSV? How to ensure correct ordering?
            //       Can we sort based on the cdm colums?

            var csvOptions = new CsvDataReaderOptions
            {
                Schema = new CsvSchema(sqlTableSchema)
            };

            using var csv = CsvDataReader.Create(streamReader, csvOptions);

            sqlPersistence.InsertCsvData(tableName, sqlConnection, csv);
        }
    }
}
