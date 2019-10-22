using System;
using System.Runtime.Serialization;

namespace Integration.Common.Model
{
    [DataContract]
    public class ActorIdentity
    {

        [DataMember]
        public string ActorId { get; set; }

        [DataMember]
        public string ActorServiceUri { get; set; }

        public ActorIdentity(string actorId, string actorServiceUri)
        {
            ActorId = actorId;
            ActorServiceUri = actorServiceUri;
            IsValid();
        }

        public bool IsValid()
        {
            if (ActorServiceUri != null)
            {
                var isValid = ActorServiceUri.Contains("fabric:/");
                if (isValid) return true;
            }
            throw new ArgumentException(
                $"ActorService provided in actor identity is not correct. Current is {ActorServiceUri}. Please consider adding full uri with application name");
        }

        public override string ToString()
        {
            return $"ActorId {ActorId} of Service {ActorServiceUri}";
        }
    }

    [DataContract]
    public class ActorServiceIdentity : ActorIdentity
    {

        public ActorServiceIdentity(string actorServiceUri) : base(null, actorServiceUri)
        {
            ActorServiceUri = actorServiceUri;
        }
    }

    [DataContract]
    public class ActorIdentityWithActionName : ActorIdentity
    {
        [DataMember]
        public string ActionName { get; set; }

        public ActorIdentityWithActionName(ActorIdentity actorIdentity, string actionName) : base(actorIdentity.ActorId, actorIdentity.ActorServiceUri)
        {
            ActionName = actionName;
        }

        public ActorIdentityWithActionName(string actorId, string actorServiceUri, string actionName) : base(actorId, actorServiceUri)
        {
            ActionName = actionName;
        }
    }


    [DataContract]
    public class ActorServiceIdentityWithActionName : ActorServiceIdentity
    {

        [DataMember]
        public string ActionName { get; set; }

        public ActorServiceIdentityWithActionName(string actorServiceUri, string actionName) : base(actorServiceUri)
        {
            ActorServiceUri = actorServiceUri;
            ActionName = actionName;
        }
    }
}
