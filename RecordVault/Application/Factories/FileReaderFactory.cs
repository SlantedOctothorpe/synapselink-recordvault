using Microsoft.Extensions.DependencyInjection;
using RecordVault.Infrastructure.FileProcessing.Common;
using RecordVault.Infrastructure.FileProcessing.CSV;
using RecordVault.Infrastructure.FileProcessing.Parquet;

namespace RecordVault.Application.Factories;

public class FileReaderFactory
{
    private readonly IServiceProvider _serviceProvider;

    public FileReaderFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IFileReader GetFileReader(string fileType)
    {
        return fileType.ToLower() switch
        {
            "csv" => _serviceProvider.GetRequiredService<CSVFileReader>(),
            "parquet" => _serviceProvider.GetRequiredService<ParquetFileReader>(),
            _ => throw new ArgumentException($"Unsupported file type: {fileType}", nameof(fileType))
        };
    }
}