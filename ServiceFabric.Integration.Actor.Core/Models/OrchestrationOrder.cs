using System;
using System.Runtime.Serialization;

namespace Integration.Common.Model
{
    [DataContract]
    public class OrchestrationOrder
    {
        [DataMember(Name = "ActorServiceUri")]
        public string ActorServiceUri { get; set; }

        [DataMember(Name = "ActorId")]
        public string ActorId { get; set; }

        [DataMember(Name = "Condition")]
        public string Condition;

        public OrchestrationOrder()
        {

        }
        public OrchestrationOrder(string actorServiceUri, string actorId) : this()
        {
            ActorId = actorId;
            ActorServiceUri = actorServiceUri;
        }

        public OrchestrationOrder(string actorServiceUri) : this()
        {
            if (!actorServiceUri.Contains("fabric"))
            {
                throw new OrchestrationOrderInvalidActorServiceUriException(
                    $"failed to created orchestration order with this name of service {actorServiceUri}. It requires full name service starting with fabric:/");
            }

            //ActorId = Guid.NewGuid().ToString();
            ActorServiceUri = actorServiceUri;
        }

        public class OrchestrationOrderInvalidActorServiceUriException : Exception
        {
            public OrchestrationOrderInvalidActorServiceUriException(string message) : base(message)
            {

            }
        }
    }

    [DataContract]
    public class ExecutableOrchestrationOrder : OrchestrationOrder
    {

    }
}