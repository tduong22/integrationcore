using Comvita.Common.EventBus.Events;
using Newtonsoft.Json;

namespace Comvita.Common.Actor.Models
{
    public class DefaultIntegrationEvent : IntegrationEvent
    {
        public string Data {get;set;}

        public string DYNAMIC_EVENT_NAME_CONST;

        public DefaultIntegrationEvent()
        {

        }

        public DefaultIntegrationEvent(object data, string eventNameConst)
        {
            Data = JsonConvert.SerializeObject(data);
            DYNAMIC_EVENT_NAME_CONST = eventNameConst;
            DynamicEventName = eventNameConst;
        }
    }
}
