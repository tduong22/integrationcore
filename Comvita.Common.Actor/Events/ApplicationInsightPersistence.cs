using Microsoft.ApplicationInsights;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Events
{
    public class ApplicationInsightPersistence : IEventPersister
    {
        private readonly TelemetryClient _telemetryClient;
        public ApplicationInsightPersistence(TelemetryClient telemetryClient)
        {
            _telemetryClient = telemetryClient;
        }

        public Task PersistErrorEventAsync(ErrorIntegrationEvent @event)
        {
            _telemetryClient.TrackEvent(@event.DynamicEventName, new Dictionary<string, string>() {
                {"Id", @event.Id.ToString() },
                {"CorrelationId", @event.CorrelationId?.ToString()},
                {"CreatationDate", @event.CreationDate.ToString()},
                {"IntegrationName", @event.IntegrationName?.ToString()},
                {"Status", @event.IntegrationStatus},
                {"Type", @event.Type.ToString()},
                {"Payload", @event.Payload }}
            );
            return Task.CompletedTask;
        }

        public Task PersistInfoEventAsync(InfoIntegrationEvent @event)
        {
            _telemetryClient.TrackEvent(@event.DynamicEventName, new Dictionary<string, string>() {
                {"Id", @event.Id.ToString() },
                {"CorrelationId", @event.CorrelationId?.ToString()},
                {"CreatationDate", @event.CreationDate.ToString()},
                {"IntegrationName", @event.IntegrationName?.ToString()},
                {"Status", @event.IntegrationStatus},
                {"Type", @event.Type?.ToString()},
                {"Payload", @event.Payload }}
           );
            return Task.CompletedTask;
        }
    }
}
