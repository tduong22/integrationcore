using System;
using System.Runtime.Serialization;

namespace Integration.Common.Flow
{
    [DataContract]
    public class FlowInfo
    {
        [DataMember]
        public FlowInstanceId FlowInstanceId { get; set; }

        [DataMember]
        public string FlowStorageServiceUrl { get; set; }

        [DataMember]
        public DateTime StartDate { get; set; }

        [DataMember]
        public string UserCreated { get; set; }
    }
}
