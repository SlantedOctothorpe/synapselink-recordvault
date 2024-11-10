using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.ValueObjects;

using System.Diagnostics;

namespace RecordVault.Infrastructure.Persistence
{
    public class AzureStorageAccountPersistence : IAzureStorageAccountPersistence
    {
        public async Task<Stream> GetStreamFromURL(AzureStorageURL storageURL)
        {
            var blobServiceClient = new BlobServiceClient(storageURL.StorageAccountUri(), new DefaultAzureCredential());
            var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageURL.Container);
            var blobClient = blobContainerClient.GetBlobClient(storageURL.GetBlobPath());

            //var stopwatch = Stopwatch.StartNew();

            var blobStream = await blobClient.OpenReadAsync();

            //BlobDownloadInfo blobDownloadInfo = await blobClient.DownloadAsync();

            //// Create a MemoryStream to hold the downloaded content
            //var memoryStream = new MemoryStream();
            //await blobDownloadInfo.Content.CopyToAsync(memoryStream);

            //// Reset the position of the stream to the beginning
            //memoryStream.Position = 0;

            //stopwatch.Stop();

            //Console.WriteLine($"Downloaded in {stopwatch.ElapsedMilliseconds}ms");

            return blobStream;
            //return memoryStream;
        }
    }
}