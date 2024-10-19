using Microsoft.Extensions.Logging;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using System.Collections.Concurrent;
using System.Data;

namespace RecordVault.Infrastructure.FileProcessing.Common;

public class Consumer : IConsumer
{
    private readonly ILogger<Consumer> _logger;

    public Consumer(ILogger<Consumer> logger)
    {
        _logger = logger;
    }

    public async Task ConsumeAsync(string tableName, IDbConnection dbConnection, ISQLPersistence sqlPersistence, BlockingCollection<List<object[]>> dataQueue)
    {
        try
        {
            foreach (var batch in dataQueue.GetConsumingEnumerable())
            {
                try
                {
                    await sqlPersistence.InsertDataAsync(tableName, dbConnection, batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error inserting batch into {tableName}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming data");
        }
    }
}