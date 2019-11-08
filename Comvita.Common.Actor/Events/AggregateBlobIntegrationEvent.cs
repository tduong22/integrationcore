using Comvita.Common.Actor.Models;
using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Events
{
    public class AggregateBlobIntegrationEvent : IntegrationEvent
    {
        public BlobStorageFileInfo BlobStorageFileInfo { get; set; }

        public AggregateBlobIntegrationEvent(BlobStorageFileInfo blobStorageFileInfo)
        {
            DynamicEventName = typeof(AggregateBlobIntegrationEvent).ToString();
            BlobStorageFileInfo = blobStorageFileInfo;
        }

        public const string DYNAMIC_EVENT_NAME_CONST = "AggregateBlobIntegrationEvent";
    }
}
