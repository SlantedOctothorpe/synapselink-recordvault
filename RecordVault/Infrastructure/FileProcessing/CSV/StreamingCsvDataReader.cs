using RecordVault.Infrastructure.FileProcessing.Common;
using Sylvan.Data.Csv;
using System.Data;

namespace RecordVault.Infrastructure.FileProcessing.CSV;

public class StreamingCsvDataReader : IStreamingDataReader
{
    private readonly CsvDataReader _csvDataReader;
    private readonly StreamReader _streamReader;
    private bool _disposed;

    public StreamingCsvDataReader(CsvDataReader csvDataReader, StreamReader streamReader)
    {
        _csvDataReader = csvDataReader;
        _streamReader = streamReader;
    }

    // We need to explicitly implement all the IDataReader members because we can't inherit from CsvDataReader.
    // Each member just forwards to the corresponding member of the wrapped CsvDataReader instance.
    public bool IsClosed => _csvDataReader.IsClosed;
    public int Depth => _csvDataReader.Depth;
    public int RecordsAffected => _csvDataReader.RecordsAffected;
    public int FieldCount => _csvDataReader.FieldCount;
    public bool Read() => _csvDataReader.Read();
    public void Close() => _csvDataReader.Close();
    public bool NextResult() => _csvDataReader.NextResult();
    public bool IsDBNull(int i) => _csvDataReader.IsDBNull(i);
    public object GetValue(int i) => _csvDataReader.GetValue(i);
    public int GetValues(object[] values) => _csvDataReader.GetValues(values);
    public string GetName(int i) => _csvDataReader.GetName(i);
    public string GetDataTypeName(int i) => _csvDataReader.GetDataTypeName(i);
    public Type GetFieldType(int i) => _csvDataReader.GetFieldType(i);
    public int GetOrdinal(string name) => _csvDataReader.GetOrdinal(name);
    public bool GetBoolean(int i) => _csvDataReader.GetBoolean(i);
    public byte GetByte(int i) => _csvDataReader.GetByte(i);
    public long GetBytes(int i, long fieldOffset, byte[]? buffer, int bufferOffset, int length)
        => _csvDataReader.GetBytes(i, fieldOffset, buffer, bufferOffset, length);
    public char GetChar(int i) => _csvDataReader.GetChar(i);
    public long GetChars(int i, long fieldOffset, char[]? buffer, int bufferOffset, int length)
        => _csvDataReader.GetChars(i, fieldOffset, buffer, bufferOffset, length);
    public Guid GetGuid(int i) => _csvDataReader.GetGuid(i);
    public short GetInt16(int i) => _csvDataReader.GetInt16(i);
    public int GetInt32(int i) => _csvDataReader.GetInt32(i);
    public long GetInt64(int i) => _csvDataReader.GetInt64(i);
    public float GetFloat(int i) => _csvDataReader.GetFloat(i);
    public double GetDouble(int i) => _csvDataReader.GetDouble(i);
    public string GetString(int i) => _csvDataReader.GetString(i);
    public decimal GetDecimal(int i) => _csvDataReader.GetDecimal(i);
    public DateTime GetDateTime(int i) => _csvDataReader.GetDateTime(i);
    public IDataReader GetData(int i) => _csvDataReader.GetData(i);
    public DataTable? GetSchemaTable() => _csvDataReader.GetSchemaTable();
    public object this[string name] => _csvDataReader[name];
    public object this[int i] => _csvDataReader[i];

    public void Dispose()
    {
        if (!_disposed)
        {
            _csvDataReader.Dispose();
            _streamReader.Dispose();
            _disposed = true;
        }
    }

    // IAsyncDisposable
    public async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            await _csvDataReader.DisposeAsync();
            _streamReader.Dispose(); // StreamReader doesn't have DisposeAsync
            _disposed = true;
        }
    }
}