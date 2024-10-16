using Microsoft.Data.SqlClient;
using RecordVault.Domain.Persistence;
using System.Collections.Concurrent;

namespace RecordVault.Domain.Services;

public interface ICSVConsumer
{
    Task ConsumeAsync(string tableName, SqlConnection sqlConnection, ISQLPersistence sqlPersistence, BlockingCollection<List<object[]>> dataQueue);
}
