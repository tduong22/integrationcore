using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.Models;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Infrastructures.Clients
{
    public class MessagingActorClient : IActorClient
    {
        private readonly IBinaryMessageSerializer _binaryMessageSerializer;

        public MessagingActorClient(IBinaryMessageSerializer binaryMessageSerializer)
        {
            _binaryMessageSerializer = binaryMessageSerializer;
        }

        public async Task ChainNextActorAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, ActorIdentity actorIdentity, CancellationToken cancellationToken)
        {
            if (actorIdentity?.ActorId == null || actorIdentity?.ActorServiceUri == null) 
                throw new ArgumentNullException($"MessagingActorClient failed at ChainNextActorAsync due to null of {nameof(actorIdentity)}. The client could not chain to next actor which is null.");
            //put more logic on to the orchestration collection later. Now it is just get the single one
            var proxy = ActorProxy.Create<IBaseMessagingActor>(
                new ActorId(actorIdentity.ActorId),
                new Uri(actorIdentity.ActorServiceUri));

            var serializedPayload = _binaryMessageSerializer.SerializePayload(payload, typeOfPayload);
            try
            {
                await proxy.ChainProcessMessageAsync(nextActorRequestContext, serializedPayload, cancellationToken);
            }
            catch (System.Fabric.FabricServiceNotFoundException)
            {
                if (actorIdentity.ActorServiceUri.Equals(Constants.DetourConstants.DEFAULT_GENERIC_DETOUR_ACTOR,
                    StringComparison.OrdinalIgnoreCase))
                {
                    if ((dynamic)payload is DetourPayload detourPayload)
                    {
                        serializedPayload = _binaryMessageSerializer.SerializePayload(detourPayload.NextActorPayload);
                        proxy = ActorProxy.Create<IBaseMessagingActor>(
                            new ActorId(detourPayload.NextActorId),
                            new Uri(detourPayload.DefaultNextActorUri));
                        await proxy.ChainProcessMessageAsync(nextActorRequestContext, serializedPayload, cancellationToken);
                    }
                }
                throw;
            }
        }

        public Task ChainNextActorAsync<T>(ActorRequestContext nextActorRequestContext, T payload, ActorIdentity actorIdentity, CancellationToken cancellationToken)
        {
            return ChainNextActorAsync(nextActorRequestContext, payload, typeof(T), actorIdentity, cancellationToken);
        }
    }
}
