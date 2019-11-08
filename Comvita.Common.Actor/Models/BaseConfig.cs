using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public abstract class BaseSchedulerConfig
    {
        [DataMember]
        public string Id { get; set; }

        [DataMember]
        public string ApplicationName { get; set;}

        [DataMember]
        public string ActorId { get; set; }

        [DataMember]
        public string ManagerActorId { get; set; }

        [DataMember]
        public string IntegrationName { get; set; }

        [DataMember]
        public string PartitionKey { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string Payload { get; set; }

        [DataMember]
        public string Description { get; set; }

        [DataMember]
        public string Type {get;set; }

    }
}
