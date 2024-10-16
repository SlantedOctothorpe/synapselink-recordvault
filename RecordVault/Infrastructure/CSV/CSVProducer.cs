using Microsoft.Extensions.Logging;
using RecordVault.Domain.Services;
using Sylvan.Data.Csv;
using System.Collections.Concurrent;

namespace RecordVault.Infrastructure.CSV;

public class CSVProducer(ILogger<CSVProducer> logger) : ICSVProducer
{
    private readonly ILogger<CSVProducer> _logger = logger;
    private const int BatchSize = 1000;

    public async Task ProduceAsync(StreamReader streamReader, CsvDataReaderOptions csvOptions, BlockingCollection<List<object[]>> dataQueue)
    {
        try
        {
            using var csv = await CsvDataReader.CreateAsync(streamReader, csvOptions);
            var batch = new List<object[]>(BatchSize);

            while (await csv.ReadAsync())
            {
                var row = new object[csv.FieldCount];
                csv.GetValues(row);
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
            _logger.LogError(ex, "Error producing CSV data");
        }
        finally
        {
            dataQueue.CompleteAdding();
        }
    }
}
