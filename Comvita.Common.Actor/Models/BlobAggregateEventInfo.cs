using Comvita.Common.Actor.Interfaces;

namespace Comvita.Common.Actor.Models
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
