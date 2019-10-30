using Integration.Common.Flow;
using System;
using System.Runtime.Serialization;

namespace Integration.Common.Model
{
    [DataContract]
    public class ActorRequestContext
    {
        [DataMember(Name = "RequestId")]
        public string RequestId { get; set; } = Guid.NewGuid().ToString();

        [DataMember(Name = "ManagerId")]
        public string ManagerId { get; set; }

        [DataMember(Name = "ManagerService")]
        public string ManagerService { get; set; }

        [DataMember(Name = "ActionName")]
        public string ActionName { get; set; }

        [DataMember(Name = "MethodName")]
        public string MethodName { get; set; }

        [DataMember(Name = "FlowInstanceId")]
        public FlowInstanceId FlowInstanceId { get; set; }

        [DataMember(Name = "TargetActor")]
        public ActorIdentity TargetActor { get; set; }

        public ActorRequestContext()
        {
        }

        public ActorRequestContext(string managerId) : this()
        {
            ManagerId = managerId;
        }

        public ActorRequestContext(string managerId, string requestId) : this()
        {
            RequestId = requestId;
            ManagerId = managerId;
        }

        public ActorRequestContext(string managerId, string actionName, string requestId = null) : this()
        {
            ManagerId = managerId;
            ActionName = actionName;
            RequestId = requestId;
        }

        public ActorRequestContext(string managerId, string actionName, string requestId = null, FlowInstanceId flowInstanceId = null) : this()
        {
            ManagerId = managerId;
            ActionName = actionName;
            RequestId = requestId;
            FlowInstanceId = flowInstanceId;
        }

        public ActorRequestContext(string managerId, string actionName, string requestId = null, ActorIdentity targetActor = null, FlowInstanceId flowInstanceId = null) : this(managerId, actionName, requestId, flowInstanceId)
        {
            TargetActor = targetActor;
        }
    }
}