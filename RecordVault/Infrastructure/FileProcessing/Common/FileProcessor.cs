using Microsoft.Extensions.Logging;
using RecordVault.Application.Factories;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using System.Collections.Concurrent;
using System.Data;

namespace RecordVault.Infrastructure.FileProcessing.Common;

public class FileProcessor : IFileProcessingService
{
    private readonly ILogger<FileProcessor> _logger;
    private readonly SQLPersistenceFactory _sqlPersistenceFactory;
    private readonly IProducer _producer;
    private readonly IConsumer _consumer;
    private readonly FileReaderFactory _fileReaderFactory;
    private const int QueueCapacity = 100;

    public FileProcessor(
        ILogger<FileProcessor> logger,
        SQLPersistenceFactory sqlPersistenceFactory,
        IProducer producer,
        IConsumer consumer,
        FileReaderFactory fileReaderFactory)
    {
        _logger = logger;
        _sqlPersistenceFactory = sqlPersistenceFactory;
        _producer = producer;
        _consumer = consumer;
        _fileReaderFactory = fileReaderFactory;
    }

    public async Task ProcessFileToSQL(Stream fileStream, string fileType, string tableName, string connectionString, string sqlType)
    {
        var sqlPersistence = _sqlPersistenceFactory.GetSQLPersistence(sqlType);
        var sqlConnection = sqlPersistence.GetSQLConnection(connectionString);
        var sqlTableSchema = sqlPersistence.GetSQLColumnSchema(tableName, sqlConnection);

        var fileReader = _fileReaderFactory.GetFileReader(fileType);

        // We retrieve a streaming reader to eventually pass to the producer.
        await using var streamingReader = await fileReader.GetStreamingReaderAsync(fileStream, sqlTableSchema);
        await ProcessDataReaderToSQL(streamingReader, tableName, sqlConnection, sqlPersistence);
    }

    private async Task ProcessDataReaderToSQL(IDataReader dataReader, string tableName, IDbConnection sqlConnection, ISQLPersistence sqlPersistence)
    {
        var dataQueue = new BlockingCollection<List<object[]>>(QueueCapacity);
        var producerTask = _producer.ProduceAsync(dataReader, dataQueue);
        var consumerTask = _consumer.ConsumeAsync(tableName, sqlConnection, sqlPersistence, dataQueue);

        await Task.WhenAll(producerTask, consumerTask);
    }
}