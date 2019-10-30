using System;
using System.Runtime.Serialization;

namespace Integration.Common.Flow
{
    [DataContract]
    public class FlowInstanceId
    {
        [DataMember] public string Id { get; set; }
        [DataMember] public string FlowName { get; set; }

        public static FlowInstanceId NewFlowInstanceId => new FlowInstanceId() { Id = Guid.NewGuid().ToString() };

        public FlowInstanceId()
        {

        }

        public FlowInstanceId(string id) : this()
        {
            Id = id;
        }

        public FlowInstanceId(string id, string flowName) : this(id)
        {
            FlowName = flowName;
        }


        public override string ToString()
        {
            return $"FlowId: {Id} of Name: {FlowName}";
        }
    }
}