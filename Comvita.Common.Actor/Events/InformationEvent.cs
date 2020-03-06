using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Events
{

    public class InfoIntegrationEvent : IntegrationEvent
    {
        public string SystemName { get; set; }
        public string IntegrationName { get; set; }
        public string IntegrationStatus { get; set; }
        public string Owner { get; set; }
        public string Payload { get; set; }
        public string Type { get; set; }

        public EventSeverity EventSeverity { get; set; }

        public InfoIntegrationEvent() : base()
        {

        }
    }
}
