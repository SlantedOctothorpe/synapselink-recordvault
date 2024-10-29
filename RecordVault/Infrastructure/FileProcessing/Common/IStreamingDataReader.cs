using System.Data;

namespace RecordVault.Infrastructure.FileProcessing.Common
{
    public interface IStreamingDataReader : IDataReader, IAsyncDisposable
    {
        // Inherits from IDataReader members - provides all standard data reading capabilities
        // Inherits DisposeAsync from IAsyncDisposable - adds async cleanup support
    }
}
