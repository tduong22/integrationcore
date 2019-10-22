using System;

namespace Integration.Common.Model
{
    public enum TrackingMessageStatus
    {
        Started = 0,
        InProgress = 1,
        Suspended = 2,
        Completed = 3,
        Failed = 4,
        Unknown = 999
    }

    public interface ITrackingMessage
    {

    }

    public class TrackingMessage : ITrackingMessage
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string RowKey { get; set; }
        public string SourceId { get; set; }
        public string SourceSystem { get; set; }
        public TrackingMessageStatus Status { get; set; }
        public string Payload { get; set; }
        public string ExceptionMessage { get; set; }
        public string BusinessDomain { get; set; }
        public DateTime LastUtcModified { get; set; }
        public string LastActor { get; set; }
        public string Domain { get; set; }

        public string PartitionKey
        {
            get => $"{SourceSystem}_{BusinessDomain}_{SourceId}";
        }
    }

    public class FileTransformTrackingMessage : TrackingMessage
    {
        public string FileName { get; set; }
    }
}
