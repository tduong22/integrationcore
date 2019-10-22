using System;
using System.Runtime.Serialization;

namespace Integration.Common.Flow
{
    [DataContract]
    public class FlowRequest
    {
    }

    [DataContract]
    public class StartFlowRequest : FlowRequest
    {

        [DataMember]
        public FlowInstanceId FlowInstanceId { get; set; }

        [DataMember]
        public DateTime StartDate { get; set; }

        [DataMember]
        public string UserStarted { get; set; }

        [DataMember]
        public string StartedPayload { get; set; }
    }
}
