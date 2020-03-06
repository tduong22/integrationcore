using System;
using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Models
{
    public class FaultedDomainEvent : DomainEvent
    {
        public string ExceptionMessage { get; }
        public string ErrorCode { get; }
        public Type ExceptionType { get; }

        public string Domain { get; }
        public string BusinessDomain { get; set; }
        public TrackingMessage TrackingMessage { get; }
        public FaultedDomainEvent(Exception exception, TrackingMessage trackingMessage)
        {
            ExceptionMessage = exception.Message;
            Domain = trackingMessage.Domain;
            TrackingMessage = trackingMessage;
            BusinessDomain = trackingMessage.BusinessDomain;
            ExceptionType = exception.GetType();
        }
    }
}
