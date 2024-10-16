using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.ValueObjects;

namespace RecordVault.Infrastructure.Persistence
{
    public class AzureStorageAccountPersistence : IAzureStorageAccountPersistence
    {
        public async Task<StreamReader> GetStreamReaderFromURL(AzureStorageURL storageURL)
        {
            var blobServiceClient = new BlobServiceClient(storageURL.StorageAccountUri(), new DefaultAzureCredential());
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageURL.Container);
            var blobClient = blobContainerClient.GetBlobClient(storageURL.GetBlobPath());

            BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();
            return new StreamReader(blobDownloadInfo.Content);
        }
    }
}
