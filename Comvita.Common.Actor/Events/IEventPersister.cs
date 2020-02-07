using System.Threading.Tasks;

namespace Comvita.Common.Actor.Events
{
    public interface IEventPersister
    {
        Task PersistInfoEventAsync(InfoIntegrationEvent @event);
        Task PersistErrorEventAsync(ErrorIntegrationEvent @event);
    }
}
