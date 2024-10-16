using Sylvan.Data.Csv;
using System.Collections.Concurrent;

namespace RecordVault.Domain.Services;

public interface ICSVProducer
{
    Task ProduceAsync(StreamReader streamReader, CsvDataReaderOptions csvOptions, BlockingCollection<List<object[]>> dataQueue);
}
