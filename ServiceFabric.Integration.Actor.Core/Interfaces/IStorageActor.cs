using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Interface
{
    public interface IStorageActor : IActor
    {
        Task<string> SaveMessageAsync(ActorRequestContext actorRequestContext, string key, byte[] payload,
            CancellationToken cancellationToken);

        Task<byte[]> RetrieveMessageAsync(ActorRequestContext actorRequestContext, string key, bool isOptional,
            CancellationToken cancellationToken);
    }
}
