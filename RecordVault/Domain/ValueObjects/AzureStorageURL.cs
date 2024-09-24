using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.Domain.ValueObjects
{
    public class AzureStorageURL
    {
        public string URL { get; private set; }
        public string StorageAccount { get; private set; }
        public string Container { get; private set; }
        public string BlobFolder { get; private set; }
        public string BlobName { get; private set; }

        public AzureStorageURL(string url)
        {
            URL = url;

            var uri = new Uri(url);
            string[] segments = uri.Segments;

            StorageAccount = uri.Host;
            Container = segments[1].Replace("/", "");
            BlobFolder = string.Join("", segments.Skip(2).Take(segments.Length - 3));
            BlobName = segments[segments.Length - 1];
        }

        public Uri StorageAccountUri()
        {
            var uriString = StorageAccount.StartsWith("https://") ? StorageAccount : $"https://{StorageAccount}";
            return new Uri(uriString);
        }

        public string GetBlobPath()
        {
            return $"{BlobFolder}{BlobName}";
        }

        public string GetBlobParentFolderName()
        {
            var folders = BlobFolder.Split("/");
            var parentName = folders.Length > 1 ? folders[folders.Length - 1] : folders[0];
            return parentName;
        }
    }
}
