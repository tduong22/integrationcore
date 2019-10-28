using Integration.Common.Actor.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.Interface;
using Integration.Common.Flow;
using Integration.Common.Actor.Model;
using Integration.Common.Model;
using Integration.Common.Interface;
using Delegates;

namespace Integration.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseAction : IAction
    {
        public string ActionName;
        protected string Id;
        protected Uri ServiceUri;
        protected IActorClient ActorClient;
        protected ILogger Logger;
        protected UnifiedActor Actor;

        /// <summary>
        /// String used for consistent logging, providing info of the current actor
        /// </summary>
        protected string CurrentActor => $"{GetType().Name} of {Id}";

        protected string NOT_ASSIGNED_FLOW_INSTANCE => Actor.NOT_ASSIGNED_FLOW_INSTANCE;

        protected FlowInstanceId CurrentFlowInstanceId => Actor.CurrentFlowInstanceId;

        protected ActorIdentityWithActionName CurrentActorIdentityWithName(string actionName) => Actor.CurrentActorIdentityWithName(actionName);

        protected IActorStateManager StateManager => Actor.StateManager;

        protected Common.Flow.Flow Flow => Actor.Flow;

        protected Step CurrentRefStep => Actor.CurrentRefStep;

        protected string ApplicationName => Actor.ApplicationName;

        protected Task<IActorReminder> RegisterReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period) => Actor.RegisterReminderAsync(reminderName, state, dueTime, period);

        protected ActorRequestContext DefaultNextActorRequestContext
        {
            get
            {
                var defaultNext = Actor.DefaultNextActorRequestContext;
                defaultNext.TargetActor = new ActorIdentity(Guid.NewGuid().ToString(), ServiceUri.ToString());
                return defaultNext;
            }
        }

        protected ActorRequestContext DefaultNextActorRequestContextWithActorId(string id)
        {
            var defaultNext = Actor.DefaultNextActorRequestContext;
            defaultNext.TargetActor = new ActorIdentity(id, ServiceUri.ToString());
            return defaultNext;
        }

        protected BaseAction() : base()
        {
        }

        public void SetActor(UnifiedActor unifiedActor)
        {
            Actor = unifiedActor ?? throw new ArgumentNullException(nameof(UnifiedActor));
            Id = unifiedActor.Id.ToString();
            ServiceUri = unifiedActor.ServiceUri;
            Logger = unifiedActor.Logger;
        }

        #region Actor Lifecycle
        //Add function to handle actor lifecycle if needed
        //Please also refer to IActivableAction
        #endregion

        #region Storage
        protected Task<string> StoreFlowMessageAsync<T>(string key, T payload, CancellationToken cancellationToken)
        => Actor.StoreFlowMessageAsync<T>(key, payload, cancellationToken);

        protected Task<T> RetrieveFlowMessageAsync<T>(string key, bool isOptional = false, CancellationToken cancellationToken = default)
        => Actor.RetrieveFlowMessageAsync<T>(key, isOptional, cancellationToken);

        #endregion

        #region Actor-Hooks
        /*
        public Task ChainNextActorsAsync<T>(ActorRequestContext nextActorRequestContext, T payload, CancellationToken cancellationToken)
        {
            return Actor.ChainNextActorsAsync(nextActorRequestContext, payload, cancellationToken);
        }

        public Task ChainNextActorsAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            return Actor.ChainNextActorsAsync(nextActorRequestContext, payload, typeOfPayload, cancellationToken);
        }*/
        public abstract Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken);
        public virtual async Task<MessageObjectResult> InternalProcessAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            if (payload is SerializableMethodInfo serializableMethodInfo)
            {
                var parameterTypes = serializableMethodInfo.Arguments.Select(c => Type.GetType(c.ArgumentAssemblyType));
                var parameters = serializableMethodInfo.Arguments.Select(x => Actor.Deserialize(Type.GetType(x.ArgumentAssemblyType), x.Value));

                if (!serializableMethodInfo.IsGenericMethod)
                {
                    var instanceDelegate = DelegateFactory.InstanceMethod(GetType(), serializableMethodInfo.MethodName, parameterTypes.ToArray());

                    if (instanceDelegate == null)
                    {
                        throw new NotImplementedException($"Delegate instance {serializableMethodInfo.MethodName} cannot be found of the current action {GetType()}. Make sure the method is implemented correctly via interface");
                    }
                    //invoke
                    await ((Task) instanceDelegate(this, parameters.ToArray())).ConfigureAwait(false);
                }
                else
                {
                    var genericTypes = serializableMethodInfo.GenericAssemblyTypes.Select(c => Type.GetType(c));
                    var genericDelegate = DelegateFactory.InstanceGenericMethod(GetType(), serializableMethodInfo.MethodName, parameterTypes.ToArray(), genericTypes.ToArray());

                    if (genericDelegate == null)
                    {
                        throw new NotImplementedException($"Delegate generic {serializableMethodInfo.MethodName} cannot be found of the current action {GetType()}. Make sure the generic method is implemented correctly via interface");
                    }
                    //invoke
                    await ((Task) genericDelegate(this, parameters.ToArray())).ConfigureAwait(false);
                }
            }
            return MessageObjectResult.None;
        }
        public virtual async Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload,
           CancellationToken cancellationToken)
        {
            //Every action only work with SerializableMethodInfo class
            await ChainRequestAsync<SerializableMethodInfo>(actorRequestContext, payload, actorRequestContext.ActionName, cancellationToken);
        }

        public Task ChainRequestAsync<T>(ActorRequestContext actorRequestContext, byte[] payload, string actionName, CancellationToken cancellationToken) => Actor.ChainRequestAsync<T>(actorRequestContext, payload, actionName, cancellationToken);

        public Task ChainRequestAsync(ActorRequestContext actorRequestContext, byte[] payload, Type typeOfPayload, string actionName, CancellationToken cancellationToken) => Actor.ChainRequestAsync(actorRequestContext, payload, typeOfPayload, actionName, cancellationToken);

        public Task ChainNextActorsAsync<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction => Actor.ChainNextActorsAsync(expression, actorRequestContext, cancellationToken);

        public Task ChainNextActorsAsync<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction => Actor.ChainNextActorsAsync(expression, actorRequestContext, executableOrchestrationOrder, cancellationToken);

        public Task ChainNextActorsAsync(ActorRequestContext actorRequestContext, object payload, Type typeOfPayload, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken) => Actor.ChainNextActorsAsync(actorRequestContext, payload, typeOfPayload, executableOrchestrationOrder, cancellationToken);
        //public Task ChainNextActorsAsync(ActorRequestContext actorRequestContext, object payload, Type typeOfPayload, CancellationToken cancellationToken) => Actor.ChainNextActorsAsync(actorRequestContext, payload, typeOfPayload, cancellationToken);
        #endregion

        #region Serialization
        public byte[] Serialize<T>(object data) => Actor.Serialize<T>(data);
        public T Deserialize<T>(byte[] data) => Actor.Deserialize<T>(data);
        #endregion

        #region Utilities
        protected virtual void DisposeActor(ActorId actorId, Uri actorServiceUri, CancellationToken cancellationToken)
        {
            Actor.DisposeActor(actorId, actorServiceUri, cancellationToken);
        }
        #endregion

    }
}
