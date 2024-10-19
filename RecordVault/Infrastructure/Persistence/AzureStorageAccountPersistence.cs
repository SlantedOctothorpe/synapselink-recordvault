using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.ValueObjects;

namespace RecordVault.Infrastructure.Persistence
{
    public class AzureStorageAccountPersistence : IAzureStorageAccountPersistence
    {
        public async Task<Stream> GetStreamFromURL(AzureStorageURL storageURL)
        {
            var blobServiceClient = new BlobServiceClient(storageURL.StorageAccountUri(), new DefaultAzureCredential());
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageURL.Container);
            var blobClient = blobContainerClient.GetBlobClient(storageURL.GetBlobPath());
            
            BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();
            
            // Create a MemoryStream to hold the downloaded content
            var memoryStream = new MemoryStream();
            await blobDownloadInfo.Content.CopyToAsync(memoryStream);
            
            // Reset the position of the stream to the beginning
            memoryStream.Position = 0;
            
            return memoryStream;
        }
    }
}