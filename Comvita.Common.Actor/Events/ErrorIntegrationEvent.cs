namespace Comvita.Common.Actor.Events
{
    public class ErrorIntegrationEvent : InfoIntegrationEvent
    {
        public string ExceptionMessage { get; set; }
        public string ExceptionType { get; set; }
        public ErrorIntegrationEvent() : base()
        {

        }
    }
}
