using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Models
{
    public class DefaultFtpDispatchIntegrationEvent : IntegrationEvent
    {
        public DispatchRequest RequestData { get; set; }

        public DefaultFtpDispatchIntegrationEvent(DispatchRequest request)
        {
            DynamicEventName = typeof(DefaultFtpDispatchIntegrationEvent).ToString();
            RequestData = request;
        }

        public const string DYNAMIC_EVENT_NAME_CONST = "DefaultFtpDispatchIntegrationEvent";
    }
}
