using Comvita.Common.EventBus.EventBusOption;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class ServiceBusConfig : BaseSchedulerConfig
    {
        [DataMember]
        public string ListenerName { get; set; }

        [DataMember]
        public bool Enabled { get; set; }

        [DataMember]
        public ServiceBusOption ServiceBusOption {get;set; }

        public ServiceBusConfig()
        {
            Id = ListenerName;
            PartitionKey = Type = nameof(ServiceBusConfig);
        }
    }
}
