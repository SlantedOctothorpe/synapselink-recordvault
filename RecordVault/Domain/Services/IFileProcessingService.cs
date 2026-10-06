namespace RecordVault.Domain.Services;

public interface IFileProcessingService
{
    Task<int> ProcessFileToSQL(Stream fileStream, string fileType, string tableName, string connectionString, string sqlType);
}
