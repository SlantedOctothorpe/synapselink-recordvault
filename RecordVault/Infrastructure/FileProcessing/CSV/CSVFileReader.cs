using RecordVault.Infrastructure.FileProcessing.Common;
using Sylvan.Data.Csv;
using System.Data;
using System.Data.Common;

namespace RecordVault.Infrastructure.FileProcessing.CSV;

public class CSVFileReader : IFileReader
{
    public async Task<IDataReader> ReadAsync(Stream stream, IEnumerable<DbColumn>? sqlTableSchema = null)
    {
        var streamReader = new StreamReader(stream);
        var csvOptions = new CsvDataReaderOptions
        {
            Schema = sqlTableSchema != null ? new CsvSchema(sqlTableSchema) : null
        };
        return await CsvDataReader.CreateAsync(streamReader, csvOptions);
    }
}