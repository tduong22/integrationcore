using System.Runtime.Serialization;
using Comvita.Common.Actor.Events;
using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class ErrorHandling
    {
        [DataMember]
        public string ServiceName { get;  set; }
        [DataMember]
        public string Payload { get;  set; }
        [DataMember]
        public string ActionName { get;  set; }
        [DataMember]
        public string Actor { get;  set; }
        [DataMember]
        public string ReminderName { get;  set; }
        [DataMember]
        public CorrelationId CorrelationId { get;  set; }
        [DataMember]
        public string OperationId { get; set; }
        [DataMember]
        public string FlowName { get; set; }
        [DataMember]
        public string ApplicationName { get; set; }
        [DataMember]
        public bool IsSendEmail { get;  set; }
        [DataMember]
        public bool IsTriggerWebApp { get;  set; }

        public ErrorHandling()
        {

        }
        public ErrorHandling(string serviceName, string payload, string actionName, string actor, string reminderName, string operationId, CorrelationId correlationId,string flowName, string applicationName, bool isSendEmail = false, bool isTriggerWebApp = true)
        {
            ServiceName = serviceName;
            Payload = payload;
            ActionName = actionName;
            Actor = actor;
            ReminderName = reminderName;
            OperationId = operationId;
            CorrelationId = correlationId;
            FlowName = flowName;
            ApplicationName = applicationName;
            IsSendEmail = isSendEmail;
            IsTriggerWebApp = isTriggerWebApp;
        }
        

    }
}
