using Integration.Common.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Interface
{
    public interface IActorClient
    {
        Task ChainNextActorAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, ActorIdentity actorIdentity, CancellationToken cancellationToken);
        Task ChainNextActorAsync<T>(ActorRequestContext nextActorRequestContext, T payload, ActorIdentity actorIdentity, CancellationToken cancellationToken);
    }
}
