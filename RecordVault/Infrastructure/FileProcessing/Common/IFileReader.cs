using System.Data;
using System.Data.Common;

namespace RecordVault.Infrastructure.FileProcessing.Common;

public interface IFileReader
{
    Task<IDataReader> ReadAsync(Stream stream, IEnumerable<DbColumn>? sqlTableSchema = null);
    Task<IStreamingDataReader> GetStreamingReaderAsync(Stream stream, IEnumerable<DbColumn>? sqlTableSchema = null);
}
