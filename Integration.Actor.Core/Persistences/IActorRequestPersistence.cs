using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Persistences
{
    public interface IActorRequestPersistence
    {
        Task SaveRequest<TModel>(string actionName, ActorRequestContext actorRequestContext, TModel entity, CancellationToken cancellationToken);
        Task SaveRequest(string actionName, ActorRequestContext actorRequestContext, Type typeOfPayload, byte[] entity, CancellationToken cancellationToken);
        Task<TModel> RetrieveRequestPayloadAsync<TModel>(string actionName, string requestId, CancellationToken cancellationToken);
        Task<Type> RetrieveRequestPayloadTypeAsync(string actionName, string requestId, CancellationToken cancellationToken);
        Task<ActorRequestContext> RetrieveRequestContextAsync(string actionName, string requestId, CancellationToken cancellationToken);
        Task<CancellationToken> RetrieveRequestCancellationToken(string actionName, string requestId);
        void SetActorStateManager(IActorStateManager actorStateManager);
        Task<IEnumerable<ActorRequestContext>> RetrieveRequestContextsAsync(CancellationToken cancellationToken);
        Task RemoveStateDataForRequestIdAsync(string actionName, string requestId, CancellationToken cancellationToken);
        Task<string> RetrieveLastReminderNameAsync();
    }
}
