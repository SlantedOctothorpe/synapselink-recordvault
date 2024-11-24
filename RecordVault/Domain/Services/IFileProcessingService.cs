namespace RecordVault.Domain.Services;

public interface IFileProcessingService
{
    Task ProcessFileToSQL(Stream fileStream, string fileType, string tableName, string connectionString, string sqlType);
}
