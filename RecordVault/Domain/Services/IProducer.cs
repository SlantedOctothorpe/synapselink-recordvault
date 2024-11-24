using System.Collections.Concurrent;
using System.Data;

namespace RecordVault.Domain.Services;

public interface IProducer
{
    Task ProduceAsync(IDataReader dataReader, BlockingCollection<List<object[]>> dataQueue);
}
