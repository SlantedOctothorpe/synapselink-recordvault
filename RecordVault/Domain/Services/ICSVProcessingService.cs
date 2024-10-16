namespace RecordVault.Domain.Services
{
    public interface ICSVProcessingService
    {
        public Task CSVStreamReaderToSQL(StreamReader streamReader, string tableName, string connectionString = "", string sqlType = "");
    }
}
