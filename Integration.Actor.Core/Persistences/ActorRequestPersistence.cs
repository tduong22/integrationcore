using Integration.Common.Actor.Helpers;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Persistences
{
    /// <summary>
    /// Help to persist the request, payload and request information to state of the actor and provide methods to retrieve
    /// </summary>
    public class ActorRequestPersistence : IActorRequestPersistence
    {
        /// <summary>
        /// Do not use these state name directly as the class supporting multiple states unless there is clear purpose (no need to have multiple state)
        /// Use these const state name in combination with request id, class id...v.v with a delimeter
        /// For reference, naming resolution with these states is built with the static class NamingCompositionResolver
        /// </summary>
        public const string REQUEST_CONTEXT_STATE_NAME = "REQUEST_CONTEXT_STATE_NAME";
        public const string PAYLOAD_STATE_NAME = "PAYLOAD_STATE_NAME";
        public const string PAYLOAD_TYPE_STATE_NAME = "PAYLOAD_TYPE_STATE_NAME";
        public const string CANCELLATION_TOKEN_STATE_NAME = "CANCELLATION_TOKEN_STATE_NAME";
        public const string REQUEST_STATES_STATE_NAME = "REQUEST_STATES_STATE_NAME";
        public const string ACTOR_REMINDER_LATEST_STATE_NAME = "ACTOR_REMINDER_LATEST_STATE_NAME";

        public const char STATE_NAME_DELIMITER = '|';

        protected IActorStateManager StateManager;
        public ActorRequestPersistence()
        {

        }

        public async Task<CancellationToken> RetrieveRequestCancellationToken(string actionName, string requestId)
        {
            return await StateManager.GetStateAsync<CancellationToken>(NameCompositionResolver.GenerateRequestCanncellationTokenStateName(actionName, requestId));
        }

        public void SetActorStateManager(IActorStateManager actorStateManager)
        {
            StateManager = actorStateManager;
        }

        public async Task<IEnumerable<ActorRequestContext>> RetrieveRequestContextsAsync(CancellationToken cancellationToken)
        {
            var result = new List<ActorRequestContext>();
            var allStateNames = await StateManager.GetStateNamesAsync(cancellationToken);
            var stateNames = allStateNames as string[] ?? allStateNames.ToArray();
            if (stateNames.Any())
            {
                var requestContextStateNames =
                    stateNames.Where(x => x.Contains(REQUEST_CONTEXT_STATE_NAME));

                foreach (var stateName in requestContextStateNames)
                {
                    var requestContext = await StateManager.GetStateAsync<ActorRequestContext>(stateName, cancellationToken);
                    result.Add(requestContext);
                }
            }
            return result;
        }

        public virtual async Task SaveRequest<TModel>(string actionName, ActorRequestContext actorRequestContext, TModel entity, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestContextStateName(actionName, actorRequestContext.RequestId), actorRequestContext, cancellationToken);

                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestPayloadStateName(actionName, actorRequestContext.RequestId), entity, cancellationToken);

                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestPayloadTypeStateName(actionName, actorRequestContext.RequestId), (object)null, cancellationToken);

                //As cancellation token cant be serialized & replicated to other node and cant be used to invoke cancellation
                //await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestCanncellationTokenStateName(actionName, actorRequestContext.RequestId), cancellationToken, cancellationToken);

                //UnComment if wantting to deal with status of the requests
                /*
                await StateManager.AddStateAsync(
                    NameCompositionResolver.GenerateRequestStateStateName(actionName,
                        actorRequestContext.RequestId), false, cancellationToken);
                //add current latest reminder name for any current info. due to the single thread behaviour of actors, this is valid
                await StateManager.AddOrUpdateStateAsync(ACTOR_REMINDER_LATEST_STATE_NAME, NameCompositionResolver.GenerateReminderName(actionName, actorRequestContext.RequestId),
                    (s, reminderName) => reminderName, cancellationToken);
                */

            }
            catch (InvalidOperationException ex)
            {
                //add an already existed state
                //throw;
            }
        }

        public virtual async Task SaveRequest(string actionName, ActorRequestContext actorRequestContext, Type typeOfPayload, byte[] entity, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestContextStateName(actionName, actorRequestContext.RequestId), actorRequestContext, cancellationToken);

                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestPayloadStateName(actionName, actorRequestContext.RequestId), entity, cancellationToken);

                await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestPayloadTypeStateName(actionName, actorRequestContext.RequestId), typeOfPayload.AssemblyQualifiedName, cancellationToken);

                //As cancellation token cant be serialized & replicated to other node and cant be used to invoke cancellation
                //await StateManager.AddStateAsync(NameCompositionResolver.GenerateRequestCanncellationTokenStateName(actionName, actorRequestContext.RequestId), cancellationToken, cancellationToken);

                //UnComment if wantting to deal with status of the requests
                /*
                await StateManager.AddStateAsync(
                    NameCompositionResolver.GenerateRequestStateStateName(actionName,
                        actorRequestContext.RequestId), false, cancellationToken);
                //add current latest reminder name for any current info. due to the single thread behaviour of actors, this is valid
                await StateManager.AddOrUpdateStateAsync(ACTOR_REMINDER_LATEST_STATE_NAME, NameCompositionResolver.GenerateReminderName(actionName, actorRequestContext.RequestId),
                    (s, reminderName) => reminderName, cancellationToken);
                    */

            }
            catch (InvalidOperationException ex)
            {
                //add an already existed state
                //throw;
            }
        }

        public async Task<TModel> RetrieveRequestPayloadAsync<TModel>(string actionName, string requestId, CancellationToken cancellationToken)
        {
            return await StateManager.GetStateAsync<TModel>(NameCompositionResolver.GenerateRequestPayloadStateName(actionName, requestId));
        }

        public async Task<Type> RetrieveRequestPayloadTypeAsync(string actionName, string requestId, CancellationToken cancellationToken)
        {
            try
            {
                var fullNameType = await StateManager.GetStateAsync<string>(NameCompositionResolver.GenerateRequestPayloadTypeStateName(actionName, requestId));
                if (fullNameType == null) return null;
                return Type.GetType(fullNameType);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public async Task<ActorRequestContext> RetrieveRequestContextAsync(string actionName, string requestId, CancellationToken cancellationToken)
        {
            return await StateManager.GetStateAsync<ActorRequestContext>(NameCompositionResolver.GenerateRequestContextStateName(actionName, requestId));
        }
        public async Task RemoveStateDataForRequestIdAsync(string actionName, string requestId, CancellationToken cancellationToken)
        {
            var allStateNames = await StateManager.GetStateNamesAsync(cancellationToken);
            var stateNames = allStateNames as string[] ?? allStateNames.ToArray();
            if (stateNames.Any())
            {
                var statesOfTheRequest =
                    stateNames.Where(x => x.EndsWith(requestId));
                foreach (var stateName in statesOfTheRequest)
                {
                    await StateManager.RemoveStateAsync(stateName, cancellationToken);
                }
            }
        }


        public async Task<string> RetrieveLastReminderNameAsync()
        {
            return await StateManager.GetStateAsync<string>(ACTOR_REMINDER_LATEST_STATE_NAME);
        }
    }
}
