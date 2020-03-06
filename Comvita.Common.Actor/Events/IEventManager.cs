using System.Threading.Tasks;

namespace Comvita.Common.Actor.Events
{
    public interface IEventManager
    {
        Task EmitInformationEventAsync(InfoIntegrationEvent @event);
        Task EmitErrorEventAsync(ErrorIntegrationEvent @event);
        IEventManager AddEventPersisters(IEventPersister @eventPersister);
    }
}
