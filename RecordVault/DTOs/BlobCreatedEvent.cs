using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RecordVault.DTOs
{

    public class BlobCreatedEvent
    {
        public string topic { get; set; }
        public string subject { get; set; }
        public string eventType { get; set; }
        public string id { get; set; }
        public BlobCreatedEventData data { get; set; }
        public string dataVersion { get; set; }
        public string metadataVersion { get; set; }
        public DateTimeOffset eventTime { get; set; }
    }

    public class BlobCreatedEventData
    {
        public string api { get; set; }
        public string clientRequestId { get; set; }
        public string requestId { get; set; }
        public string eTag { get; set; }
        public string contentType { get; set; }
        public int contentLength { get; set; }
        public string blobType { get; set; }
        public string accessTier { get; set; }
        public string blobUrl { get; set; }
        public string url { get; set; }
        public string sequencer { get; set; }
        public string identity { get; set; }
        public BlobCreatedEventStoragediagnostics storageDiagnostics { get; set; }
    }

    public class BlobCreatedEventStoragediagnostics
    {
        public string batchId { get; set; }
    }
}
