using Integration.Common.Interface;

namespace Integration.Common.Model
{
    public class BlobAggregateEventInfo : BlobStorageFileInfo, IPartitionable
    {
        public string Domain { get; set; }
        public string ExtractPartitionKey()
        {
            return Domain;
        }
    }
}
