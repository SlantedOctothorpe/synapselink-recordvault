using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.Services;
using System.Collections.Concurrent;

namespace RecordVault.Infrastructure.CSV;

public class CSVConsumer(ILogger<CSVConsumer> logger) : ICSVConsumer
{
    private readonly ILogger<CSVConsumer> _logger = logger;

    public async Task ConsumeAsync(string tableName, SqlConnection sqlConnection, ISQLPersistence sqlPersistence, BlockingCollection<List<object[]>> dataQueue)
    {
        try
        {
            foreach (var batch in dataQueue.GetConsumingEnumerable())
            {
                try
                {
                    await sqlPersistence.InsertCsvDataAsync(tableName, sqlConnection, batch);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error inserting batch into {tableName}");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error consuming CSV data");
        }
    }
}
