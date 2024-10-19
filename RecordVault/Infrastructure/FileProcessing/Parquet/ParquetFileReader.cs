using RecordVault.Infrastructure.FileProcessing.Common;
using System.Data;
using System.Data.Common;

namespace RecordVault.Infrastructure.FileProcessing.Parquet
{
    public class ParquetFileReader : IFileReader
    {
        public Task<IDataReader> ReadAsync(Stream stream, IEnumerable<DbColumn>? sqlTableSchema = null)
        {
            // Use a Parquet library
            throw new NotImplementedException("Parquet reading not yet implemented");
        }
    }
}
