using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Events
{
    public class DefaultEventManager : IEventManager
    {
        private readonly List<IEventPersister> _eventPersisters;

        private readonly ILogger _logger;
        public DefaultEventManager(ILogger logger)
        {
            _logger = logger;
            _eventPersisters = new List<IEventPersister>();
        }

        public virtual async Task EmitInformationEventAsync(InfoIntegrationEvent @event)
        {
            _logger.LogInformation($"{nameof(IEventManager)} emitted event of {@event.DynamicEventName} at {@event.CreationDate}");

            foreach (var eventPersister in _eventPersisters)
            {
                await eventPersister.PersistInfoEventAsync(@event);
            }
        }

        public virtual async Task EmitErrorEventAsync(ErrorIntegrationEvent @event)
        {
            _logger.LogError($"{nameof(IEventManager)} emitted Error event of {@event.DynamicEventName} at {@event.CreationDate}");

            foreach (var eventPersister in _eventPersisters)
            {
                await eventPersister.PersistErrorEventAsync(@event);
            }
        }

        public IEventManager AddEventPersisters(IEventPersister eventPersister)
        {
            _eventPersisters.Add(eventPersister);
            return this;
        }
    }
}
