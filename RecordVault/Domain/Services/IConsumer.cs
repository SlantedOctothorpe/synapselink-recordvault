using Microsoft.Data.SqlClient;
using RecordVault.Domain.Persistence;
using System.Collections.Concurrent;
using System.Data;

namespace RecordVault.Domain.Services;

public interface IConsumer
{
    Task ConsumeAsync(string tableName, IDbConnection dbConnection, ISQLPersistence sqlPersistence, BlockingCollection<List<object[]>> dataQueue);
}