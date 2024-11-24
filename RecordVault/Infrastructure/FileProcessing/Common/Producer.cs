using Microsoft.Extensions.Logging;
using RecordVault.Domain.Services;
using System.Collections.Concurrent;
using System.Data;

namespace RecordVault.Infrastructure.FileProcessing.Common;

public class Producer : IProducer
{
    private readonly ILogger<Producer> _logger;
    private const int BatchSize = 1000;

    public Producer(ILogger<Producer> logger)
    {
        _logger = logger;
    }

    public async Task ProduceAsync(IDataReader dataReader, BlockingCollection<List<object[]>> dataQueue)
    {
        try
        {
            var batch = new List<object[]>(BatchSize);
            while (await Task.Run(() => dataReader.Read()))
            {
                var row = new object[dataReader.FieldCount];
                dataReader.GetValues(row);
                batch.Add(row);
                if (batch.Count >= BatchSize)
                {
                    dataQueue.Add(batch);
                    batch = new List<object[]>(BatchSize);
                }
            }
            if (batch.Count > 0)
            {
                dataQueue.Add(batch);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error producing data");
        }
        finally
        {
            dataQueue.CompleteAdding();
        }
    }
}