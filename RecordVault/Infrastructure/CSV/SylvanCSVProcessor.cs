using Microsoft.Extensions.Logging;
using RecordVault.Application.Factories;
using RecordVault.Domain.Services;
using Sylvan.Data.Csv;
using System.Collections.Concurrent;

namespace RecordVault.Infrastructure.CSV;

public class SylvanCSVProcessor(
ILogger<SylvanCSVProcessor> logger,
SQLPersistenceFactory sqlPersistenceFactory,
ICSVProducer csvProducer,
ICSVConsumer csvConsumer) : ICSVProcessingService
{
    private readonly ILogger<SylvanCSVProcessor> _logger = logger;
    private readonly SQLPersistenceFactory _sqlPersistenceFactory = sqlPersistenceFactory;
    private readonly ICSVProducer _csvProducer = csvProducer;
    private readonly ICSVConsumer _csvConsumer = csvConsumer;

    private const int QueueCapacity = 100;

    public async Task CSVStreamReaderToSQL(StreamReader streamReader, string tableName, string connectionString = "", string sqlType = "")
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

        var dataQueue = new BlockingCollection<List<object[]>>(QueueCapacity);

        var producerTask = _csvProducer.ProduceAsync(streamReader, csvOptions, dataQueue);
        var consumerTask = _csvConsumer.ConsumeAsync(tableName, sqlConnection, sqlPersistence, dataQueue);

        await Task.WhenAll(producerTask, consumerTask);
    }
}
