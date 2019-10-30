using Integration.Common.Interface;
using MessagePack;
using System;
using System.Runtime.Serialization;

namespace Integration.Common.Model
{
    [DataContract]
    public class DispatchRequest<T> : DispatchRequest where T : IPartitionable
    {
        public DispatchRequest()
        {

        }
        public DispatchRequest(T requestData, string domain, ActorIdentityWithActionName responseActor, string dispatchActionName) : this(requestData, typeof(T), domain, responseActor, dispatchActionName)
        {

        }

        public DispatchRequest(object requestData, Type typeOfPayload, string domain, ActorIdentityWithActionName responseActor, string dispatchActionName) : base(requestData, typeOfPayload, domain, responseActor, dispatchActionName)
        {
        }
    }

    [KnownType(typeof(byte[]))]
    [DataContract]
    public class DispatchRequest
    {
        [DataMember]
        public string TypeFullName { get; set; }

        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public byte[] RequestDataBinary { get; set; }

        [DataMember]
        public ActorIdentityWithActionName ResponseActorInfo { get; set; }

        [DataMember]
        public string DispatchActionName { get; set; }

        [DataMember]
        public string PartitionKey { get; set; }

        public DispatchRequest()
        {

        }

        public DispatchRequest(object requestData, Type typeOfPayload, string domain, ActorIdentityWithActionName responseActor, string dispatchActionName)
        {
            RequestDataBinary = MessagePackSerializer.NonGeneric.Serialize(typeOfPayload, requestData);
            Domain = domain;
            if (requestData is IPartitionable partitionable)
            {
                PartitionKey = partitionable.ExtractPartitionKey();
            }
            TypeFullName = typeOfPayload.AssemblyQualifiedName;
            ResponseActorInfo = responseActor;
            DispatchActionName = dispatchActionName;
        }

        public object DeserializeRequestData()
        {
            return MessagePackSerializer.NonGeneric.Deserialize(Type.GetType(TypeFullName), RequestDataBinary);
        }
    }
}