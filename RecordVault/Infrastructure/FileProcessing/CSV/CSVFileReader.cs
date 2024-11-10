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
            HasHeaders = false,
            Schema = sqlTableSchema != null ? new CsvSchema(sqlTableSchema) : null,
            // This is necessary because the provided CSV files have empty strings for false values (at least for IsDelete)
            // CsvDataReader will throw an exception if it encounters an empty string when trying to parse a boolean
            // This assumption is based on the provided CSV files where the only boolean column is IsDelete
            // If this assumption is incorrect, this will need to be updated to implement a custom IDataReader wrapper around the CSVDataReader
            FalseString = "",
            TrueString = "True"
        };
        var reader = await CsvDataReader.CreateAsync(streamReader, csvOptions);
        return reader;
    }
}