using Autofac;
using Integration.Common.Actor.BaseActor;
using Integration.Common.Actor.Interface;
using Integration.Common.Actor.Model;
using Integration.Common.Actor.Persistences;
using Integration.Common.Flow;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.Integration.Actor.Core.Helpers;
using System;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.UnifiedActor
{
    public abstract class UnifiedActor : NonBlockingActor, IStorageActor
    {
        private IAction _action;
        public IAction Action
        {
            get => _action;
            set
            {   //when IAction is set, also set the reference of the actor to IAction
                //used to reference to the actor's properties inside IAction
                if (value != null)
                {
                    _action = value;
                    _action.SetActor(this);
                }
            }
        }

        public new string NOT_ASSIGNED_FLOW_INSTANCE => "NOT_ASSIGNED_FLOW_INSTANCE";

        //public API wrapper
        public new IActorRequestPersistence ActorRequestPersistence => base.ActorRequestPersistence;

        public new FlowInstanceId CurrentFlowInstanceId => base.CurrentFlowInstanceId;
        public new ActorRequestContext CurrentRequestContext => base.CurrentRequestContext;

        public new ActorRequestContext DefaultNextActorRequestContext => base.DefaultNextActorRequestContext;

        public new ActorRequestContext DefaultNextActorRequestContextWithActionName(string actionName) => base.DefaultNextActorRequestContextWithActionName(actionName);

        public new ActorIdentity CurrentActorIdentity => base.CurrentActorIdentity;
        public new ActorIdentity CurrentActorServiceWithActorIdNotSpecified => base.CurrentActorServiceWithActorIdNotSpecified;

        public new Common.Flow.Flow Flow => base.Flow;

        public new Step CurrentRefStep => base.CurrentRefStep;

        public new ILogger Logger => base.Logger;

        public new Step NextStep => base.NextStep;

        public new Task<IActorReminder> RegisterReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period) => base.RegisterReminderAsync(reminderName, state, dueTime, period);

        public new ActorIdentityWithActionName CurrentActorIdentityWithName(string actionName)
        {
            return base.CurrentActorIdentityWithName(actionName);
        }

        public new Task<string> StoreFlowMessageAsync<T>(string key, object payload, CancellationToken cancellationToken) => base.StoreFlowMessageAsync<T>(key, payload, cancellationToken);

        public new Task<T> RetrieveFlowMessageAsync<T>(string key, bool isOptional, CancellationToken cancellationToken) => base.RetrieveFlowMessageAsync<T>(key, isOptional, cancellationToken);

        public new void DisposeActor(ActorId actorId, Uri actorServiceUri, CancellationToken cancellationToken) => base.DisposeActor(actorId, actorServiceUri, cancellationToken);

        public new Task ChainNextActorsAsync(ActorRequestContext actorRequestContext, object payload, Type typeOfPayload, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken) => base.ChainNextActorsAsync(actorRequestContext, payload, typeOfPayload, executableOrchestrationOrder, cancellationToken);


        protected ILifetimeScope lifetimeScope;

        protected UnifiedActor(ActorService actorService, ActorId actorId,
                               IActorRequestPersistence actorRequestPersistence,
                               IBinaryMessageSerializer binaryMessageSerializer, IActorClient actorClient,
                               IKeyValueStorage<string> storage, ILogger logger)
            : base(actorService, actorId, actorRequestPersistence, binaryMessageSerializer, actorClient, storage, logger)
        {
            lifetimeScope = CoreDependencyResolver.CreateLifetimeScope();
        }

        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            await TryResolveIActionByActorId();

            //after trying to resolve from OnActivateAsync, invoke the IAction's OnActivateAsync
            if (Action != null && Action is IActivatableAction activatableAction)
            {
                await activatableAction.OnActivateAsync();
            }
        }

        private Task TryResolveIActionByActorId()
        {
            // Resolve key of IAction via ActorId by extract the interface name between parentheses ( )
            // Not support for nested
            var stringId = Id.ToString();
            if (stringId.Contains("(") && stringId.Contains(")"))
            {
                var keyToResolveIAction = Regex.Match(stringId, @"\(([^)]*)\)").Groups[1].Value;
                Action = lifetimeScope.ResolveOptionalKeyed<IAction>(keyToResolveIAction);
            }
            return Task.CompletedTask;
        }

        protected override async Task OnDeactivateAsync()
        {
            if (Action is IActivatableAction activatableAction)
            {
                await activatableAction.OnDeactivateAsync();
            }
            lifetimeScope.Dispose();
            await base.OnDeactivateAsync();
        }

        protected override async Task<MessageObjectResult> InternalProcessAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            try
            {
                Action = lifetimeScope.ResolveKeyed<IAction>(actionName);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to resolve IAction with actionName {actionName} on InternalProcessAsync. Message: {ex.Message}");
                throw;
            }

            await Action.InternalProcessAsync(actionName, payload, cancellationToken);
            return MessageObjectResult.None;
        }

        public override async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            // storage
            if (reminderName.Contains(TTL_REMINDER_NAME))
            {
                Logger.LogInformation($"Releasing resource for actor id {Id.ToString()}");
                await StateManager.TryRemoveStateAsync(TTL_REMINDER_NAME);
            }
            else
            {
                await base.ReceiveReminderAsync(reminderName, state, dueTime, period);

                if (Action is IRemindableAction remindableAction)
                    await remindableAction.ReceiveReminderAsync(reminderName, state, dueTime, period);
            }
        }

        public byte[] Serialize<T>(object data) => Serialize(typeof(T), data);
        public T Deserialize<T>(byte[] data) => (T)Deserialize(typeof(T), data);

        public byte[] Serialize(Type type, object data)
        {
            return SerializePayload(data, type);
        }

        public object Deserialize(Type type, byte[] data)
        {
            return DeserializePayload(data, type);
        }

        /// <summary>
        /// A new method to allow actions to invoke a call to the next actor via TIActionInterface
        /// </summary>
        /// <typeparam name="TIActionInterface"></typeparam>
        /// <param name="expression"></param>
        /// <param name="actorRequestContext"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public Task ChainNextActorsAsync<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction
        {
            return ActionInvoker.Invoke<TIActionInterface>(expression, actorRequestContext, cancellationToken);
        }

        public Task ChainNextActorsAsync<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction
        {
            return ActionInvoker.Invoke<TIActionInterface>(expression, actorRequestContext, executableOrchestrationOrder, cancellationToken);
        }

        public new Task ChainNextActorsAsync<T>(ActorRequestContext nextActorRequestContext, T payload, CancellationToken cancellationToken)
        {
            return this.ChainNextActorsAsync(nextActorRequestContext, payload, typeof(T), cancellationToken);
        }

        /*
        public new Task ChainNextActorsAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            //read from the NextStep and invoke chainnextactor
            var nextStep = NextStep;
            if (string.IsNullOrEmpty(nextStep.MethodName))
                //if MethodName == null, then this is not suppose to invoke the next actor via IAction
                return base.ChainNextActorsAsync(nextActorRequestContext, payload, typeOfPayload, cancellationToken);

            else return ActionInvoker.Invoke(nextStep.MethodName, nextActorRequestContext, Orders.GetFirstExecutableOrder(), cancellationToken);
        }*/

        public new Task ChainRequestAsync(ActorRequestContext actorRequestContext, byte[] payload, Type typeOfPayload, string actionName, CancellationToken cancellationToken)
        => base.ChainRequestAsync(actorRequestContext, payload, typeOfPayload, actionName, cancellationToken);

        public new Task ChainRequestAsync<T>(ActorRequestContext actorRequestContext, byte[] payload, string actionName, CancellationToken cancellationToken)
        => base.ChainRequestAsync<T>(actorRequestContext, payload, actionName, cancellationToken);

        public override async Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload, CancellationToken cancellationToken)
        {
            try
            {
                if (string.IsNullOrEmpty(actorRequestContext?.ActionName))
                    throw new InvalidOperationException($"UnifiedActors only work with a not-null actionName that used to resolve the correct IAction. Current is {actorRequestContext?.ActionName}");
                /*
                 *Add when flow is implemented
                var flow = await ResolveFlowAsync(null);
                if (flow != null)
                    await ResolveStepAsync(actorRequestContext.ActionName);*/

                Action = lifetimeScope.ResolveKeyed<IAction>(actorRequestContext.ActionName);
                await Action.ChainProcessMessageAsync(actorRequestContext, payload, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to ChainProcessMessageAsync. Message: {ex.Message}");
                await OnFailedAsync(actorRequestContext.ActionName, payload, ex, cancellationToken);
                throw;
            }
        }

        protected override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.FromResult(true);
        }

        protected override Task OnFailedAsync(string actionName, object payload, Exception exception, CancellationToken cancellationToken)
        {
            return base.OnFailedAsync(actionName, payload, exception, cancellationToken);
        }

        protected override Task OnSuccessAsync(string actionName, object payload, MessageObjectResult result,
           CancellationToken cancellationToken = default(CancellationToken))
        {
            //Unified actor only cleanup requests data because it could be used for other purposes beside handling non-blocking request
            return Task.CompletedTask;
        }

        #region Storage
        public const string SAVED_ERROR = "SAVED_ERROR";
        public const string TTL_REMINDER_NAME = "TTL_REMINDER_NAME";

        protected IActorReminder ActorReminder;
        protected int MAX_TTL_IN_MINUTE = 60;


        public async Task<string> SaveMessageAsync(ActorRequestContext actorRequestContext, string key, byte[] payload, CancellationToken cancellationToken)
        {
            try
            {
                // save message => schedule to delete after TTL
                await RegisterReminderAsync(TTL_REMINDER_NAME + key, null, TimeSpan.FromDays(MAX_TTL_IN_MINUTE), TimeSpan.FromMilliseconds(-1));
                await StateManager.AddOrUpdateStateAsync(TTL_REMINDER_NAME + key, payload, (k, v) => payload, cancellationToken);
                return key;
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Failed to store the variable with key {key}.");
                return SAVED_ERROR;
            }
        }

        public async Task<byte[]> RetrieveMessageAsync(ActorRequestContext actorRequestContext, string key, bool isOptional, CancellationToken cancellationToken)
        {
            try
            {
                //anytime a request to retrieve data, re-schedule reminder to new one
                await RegisterReminderAsync(TTL_REMINDER_NAME + key, null, TimeSpan.FromDays(MAX_TTL_IN_MINUTE), TimeSpan.FromMilliseconds(-1));
                return await StateManager.GetStateAsync<byte[]>(TTL_REMINDER_NAME + key);
            }
            catch (System.Exception ex)
            {
                if (isOptional) return null;
                Logger.LogError(ex, $"Failed to retrieve the variable with key {key} from {actorRequestContext?.ManagerId}.");
                throw;
            }
        }

        #endregion
    }
}
