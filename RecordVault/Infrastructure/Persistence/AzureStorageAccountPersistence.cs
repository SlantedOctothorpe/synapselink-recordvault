using Azure.Identity;
using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using RecordVault.Domain.Enums;
using RecordVault.Domain.Persistence;
using RecordVault.Domain.ValueObjects;

namespace RecordVault.Infrastructure.Persistence
{
    public class AzureStorageAccountPersistence : IAzureStorageAccountPersistence
    {
        public async Task<Stream> GetStreamFromURL(AzureStorageURL storageURL)
        {
            var storageAccountAuthMethodStr = Environment.GetEnvironmentVariable("AzureStorageAccountAuthMethod") ?? "";
            if (string.IsNullOrEmpty(storageAccountAuthMethodStr))
            {
                throw new ArgumentNullException("Storage Account authentication method not provided");
            }

            var storageAccountAuthMethod = Enum.TryParse<AzureStorageAccountAuthMethodEnum>(storageAccountAuthMethodStr, out var authMethodResult) ? authMethodResult : AzureStorageAccountAuthMethodEnum.ManagedIdentity;

            var storageAccountSharedKey = "";
            var storageAccountName = "";
            if (storageAccountAuthMethod == AzureStorageAccountAuthMethodEnum.SharedKey)
            {
                storageAccountSharedKey = Environment.GetEnvironmentVariable("AzureStorageAccountAccessKey") ?? "";
                if (string.IsNullOrEmpty(storageAccountSharedKey))
                {
                    throw new ArgumentNullException("Storage Account shared access key not provided");
                }

                storageAccountName = Environment.GetEnvironmentVariable("AzureStorageAccountName");
                if (string.IsNullOrEmpty(storageAccountName))
                {
                    throw new ArgumentNullException("Storage Account name not provided");
                }

            }

            var blobServiceClient = storageAccountAuthMethod switch
            {
                AzureStorageAccountAuthMethodEnum.SharedKey => new BlobServiceClient(storageURL.StorageAccountUri(), new StorageSharedKeyCredential(storageAccountName, storageAccountSharedKey)),
                AzureStorageAccountAuthMethodEnum.ManagedIdentity => new BlobServiceClient(storageURL.StorageAccountUri(), new DefaultAzureCredential()),
                _ => throw new ArgumentException($"Azure Storage Account authentication method {storageAccountAuthMethod} not supported")
            };

            var blobContainerClient = blobServiceClient.GetBlobContainerClient(storageURL.Container);
            var blobClient = blobContainerClient.GetBlobClient(storageURL.GetBlobPath());

            var blobStream = await blobClient.OpenReadAsync();

            return blobStream;
        }
    }
}