using System;
using System.Collections.Generic;
using System.Text;

namespace ServiceFabric.Integration.Actor.Core.Models
{
    public class ActorExecutionContext
    {
        public string ServiceUri { get; set; }
        public string Payload { get; set; }
        public string MethodName { get; set; }
        public string ActionName { get; set; }
        public string ActorName { get; set; }
        public string ReminderName { get; set; }
        public string OperationId { get; set; }
        public string FlowName { get; set; }
        public string ApplicationName { get; set; }
        public bool Resendable { get; set; }
        public string SourceSystem { get; set; }
        public string Entity { get; set; }
        public string EntityId { get; set; }
        public Dictionary<string, object> CustomRequestInfo { get; set; } = new Dictionary<string, object>();
    }
}
