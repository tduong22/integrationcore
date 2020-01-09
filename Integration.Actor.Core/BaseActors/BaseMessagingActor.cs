using Integration.Common.Actor.Helpers;
using Integration.Common.Actor.Interface;
using Integration.Common.Exceptions;
using Integration.Common.Extensions;
using Integration.Common.Flow;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.BaseActor
{
    public abstract class BaseMessagingActor : BaseActor, IBaseMessagingActor
    {
        /// <summary>
        /// String used for consistent logging, providing info of the current actor
        /// </summary>
        protected string CurrentActor => $"{CurrentFlowInstanceId}, {GetType().BaseType?.Name} of {Id}";

        protected BaseMessagingActor(ActorService actorService, ActorId actorId,
                                     IBinaryMessageSerializer binaryMessageSerializer,
                                     IActorClient actorClient,
                                     IKeyValueStorage<string> storage,
                                     ILogger logger) : base(actorService, actorId, logger)
        {
            /*
            Flow = BaseDependencyResolver.ResolveFlow();
            if (Flow != null)
            {
                CurrentRefStep = Flow.GetCurrentStep(CurrentActorServiceWithActorIdNotSpecified);
                NextStep = Flow.GetNextStep(CurrentRefStep);
                Orders = NextStep.Orders;
            }*/

            Orders = OrchestrationOrderCollection.NoOrder();

            //resolve binary serializer

            BinaryMessageSerializer = binaryMessageSerializer ?? throw new ArgumentNullException(nameof(binaryMessageSerializer));
            ActorClient = actorClient ?? throw new ArgumentNullException(nameof(actorClient));
            StorageService = storage;

            //resolve storage
            //StorageService = BaseDependencyResolver.ResolveStorageService<string>();

            //resolve flowservice
            //AsyncFlowService = BaseDependencyResolver.ResolveFlowService();

            //resolve actorclient to be able to chain next

        }

        #region Validation
        protected abstract Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken = default);

        /// <summary>
        /// Validate request to see if the action name is already implemented on the actor or some failure with manager id
        /// Override when there is the need of that kind validation. Possible exception that could be thrown is ActorMessageValidationException and all of its deriveds
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="cancellationToken"></param>
        /// <exception cref="ActorMessageValidationException"></exception>
        /// <exception cref="ActorRequestActionNameNotSupportedException"></exception>
        /// <returns></returns>
        protected virtual Task<bool> ValidateRequestAsync(ActorRequestContext requestContext,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
        #endregion

        #region Serialization

        protected IBinaryMessageSerializer BinaryMessageSerializer;

        protected virtual byte[] SerializePayload<T>(T entity)
        {
            try
            {
                return BinaryMessageSerializer.SerializePayload(entity);
            }
            catch (Exception ex)
            {
                throw new ActorSerializationException($"{CurrentActor} failed to serialize data of type, error message {ex.Message}", ex, typeof(T));
            }

        }

        protected virtual T DeserializePayload<T>(byte[] data)
        {
            try
            {
                return BinaryMessageSerializer.DeserializePayload<T>(data);
            }
            catch (Exception ex)
            {
                throw new ActorSerializationException($"{CurrentActor} failed to deserialize data of type, error message {ex.Message}", ex, typeof(T));
            }
        }

        protected virtual byte[] SerializePayload(object entity, Type entityType)
        {
            try
            {
                return BinaryMessageSerializer.SerializePayload(entity, entityType);
            }
            catch (Exception ex)
            {
                throw new ActorSerializationException($"{CurrentActor} failed to serialize non-generic data of type, error message {ex.Message}", ex, entityType);
            }
        }

        protected virtual object DeserializePayload(byte[] data, Type entityType)
        {
            try
            {
                return BinaryMessageSerializer.DeserializePayload(data, entityType);
            }
            catch (Exception ex)
            {
                throw new ActorSerializationException($"{CurrentActor} failed to deserialize non-generic data of type, error message {ex.Message}", ex, entityType);
            }
        }
        #endregion

        public abstract Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload,
            CancellationToken cancellationToken);

        #region Actor Orchestration
        private FlowInstanceId _currentFlowInstanceId;
        protected OrchestrationOrderCollection Orders;
        protected Common.Flow.Flow Flow;
        protected Step CurrentRefStep;
        protected Step CurrentStep;
        protected Step NextStep;

        protected const string NOT_ASSIGNED_FLOW_INSTANCE = "NOT_ASSIGNED_FLOW_INSTANCE";

        protected FlowInstanceId CurrentFlowInstanceId
        {
            get
            {   //calling this actor with no flowinstanceid, could be the start of the flow
                if (CurrentRequestContext?.FlowInstanceId == null)
                {
                    //if we ever assign a id, for multiple call currentflowinstanceid, only set once
                    if (_currentFlowInstanceId == null)
                    {
                        var autogeneratedFlowInstanceId = new FlowInstanceId()
                        { Id = Guid.NewGuid().ToString(), FlowName = NOT_ASSIGNED_FLOW_INSTANCE };
                        _currentFlowInstanceId = autogeneratedFlowInstanceId;
                    }

                }
                else
                {
                    //if calling this actor with a flowinstanceid already
                    _currentFlowInstanceId = CurrentRequestContext?.FlowInstanceId;
                }
                return _currentFlowInstanceId;
            }
        }

        /// <summary>
        /// Use ActorClient for calling other actor or services
        /// </summary>
        protected IActorClient ActorClient;

        /// <summary>
        /// Only use after reminder async. This property only represent current processing request context.
        /// </summary>
        protected ActorRequestContext CurrentRequestContext;

        /// <summary>
        /// Generate a new default actor request instance with this current actor id and the current order name as the action name
        /// </summary>
        protected ActorRequestContext DefaultNextActorRequestContext => new ActorRequestContext(Id.ToString(), Orders?.Name, Guid.NewGuid().ToString(), CurrentFlowInstanceId);

        /// <summary>
        /// Generate a new default actor request context instance with a specific action name.
        /// Note that it will ignore actionName on Order member so should be used with a custom order to chain next
        /// </summary>
        protected ActorRequestContext DefaultNextActorRequestContextWithActionName(string actionName) => new ActorRequestContext(Id.ToString(), actionName, Guid.NewGuid().ToString(), CurrentFlowInstanceId);

        protected ActorIdentity CurrentActorIdentity => new ActorIdentity(Id.ToString(), ServiceUri.ToString());
        protected ActorIdentity CurrentActorServiceWithActorIdNotSpecified => new ActorIdentity(null, ServiceUri.ToString());

        protected ActorIdentityWithActionName CurrentActorIdentityWithName(string actionName)
        {
            return new ActorIdentityWithActionName(CurrentActorIdentity, actionName);
        }

        /*
        protected Common.Flow.Flow RegisterFlow(string keyName)
        {
            Flow = BaseDependencyResolver.ResolveFlow(keyName);
            if (Flow != null)
            {
                CurrentRefStep = Flow.GetCurrentStep(CurrentActorServiceWithActorIdNotSpecified);
                NextStep = Flow.GetNextStep(CurrentRefStep);
                Orders = NextStep.Orders;
            }
            return Flow;
        }*/

        /// <summary>
        /// For custom resolve step before going next step
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="actorId"></param>
        /// <param name="occurrence"></param>
        /// <returns></returns>
        protected Step ResolveStep(string actionName, string actorId = null, int occurrence = 0)
        {
            if (string.IsNullOrEmpty(actionName))
            {
                // if having actorId then need to resolve with the correct id
                ActorIdentityWithActionName actorIdentityWithActionName = CurrentActorIdentityWithName(actionName);
                if (string.IsNullOrEmpty(actorId)) actorIdentityWithActionName.ActorId = actorId;

                CurrentRefStep = Flow.GetCurrentStep(actorIdentityWithActionName, true, occurrence);
                NextStep = Flow.GetNextStep(CurrentRefStep);
                Orders = NextStep.Orders;
            }
            else // no need action name to resolve
            {
                CurrentRefStep = Flow.GetCurrentStep(CurrentActorServiceWithActorIdNotSpecified);
                NextStep = Flow.GetNextStep(CurrentRefStep);
                Orders = NextStep.Orders;
            }
            return CurrentRefStep;
        }

        /// <summary>
        /// Resolve the correct flow by name. Used when multiple flows are being used
        /// </summary>
        /// <param name="flowName"></param>
        /// <returns></returns>
        /// 
        /*
        protected Task<Common.Flow.Flow> ResolveFlowAsync(string flowName)
        {
            Flow = BaseDependencyResolver.ResolveFlow(flowName);
            return Task.FromResult(Flow);
        }*/

        /// <summary>
        /// Resolve correct step by current flow.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="actorId"></param>
        /// <param name="occurrence"></param>
        /// <returns></returns>
        protected Task<Step> ResolveStepAsync(string actionName, int occurrence = 0)
        {
            CurrentRefStep = Flow.GetCurrentStep(CurrentActorIdentityWithName(actionName), true, occurrence);
            NextStep = Flow.GetNextStep(CurrentRefStep);
            Orders = NextStep.Orders;
            return Task.FromResult(CurrentRefStep);
        }

        /// <summary>
        /// Resolve correct step by actorid & actor service by current flow.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="actorId"></param>
        /// <param name="occurrence"></param>
        /// <returns></returns>
        protected Task<Step> ResolveStepByActorIdAsync(string actorService, string actorId, int occurrence = 0)
        {
            CurrentRefStep = Flow.GetCurrentStep(CurrentActorIdentityWithName(null), false, occurrence);
            NextStep = Flow.GetNextStep(CurrentRefStep);
            Orders = NextStep.Orders;
            return Task.FromResult(CurrentRefStep);
        }

        #region Actor Orchestration - Generics
        /// <summary>
        /// Chain to the next actor with your payload using your constructed orders
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nextActorRequestContext"></param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainNextActorsAsync<T>(ActorRequestContext nextActorRequestContext, T payload, CancellationToken cancellationToken)
        {
            // if having any request-order
            if (!Orders.IsEmpty)
            {
                //put more logic on to the orchestration collection later. Now it is just get the single one
                var anotherOrderInMyActorLifeTime =
                    Orders.GetFirstExecutableOrder();

                await ChainNextActorsAsync(nextActorRequestContext, payload, anotherOrderInMyActorLifeTime, cancellationToken);
            }
            //just do nothing because no order
        }

        /// <summary>
        /// Wrapper for orchestration-order to be converted to executable order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nextActorRequestContext"></param>
        /// <param name="payload"></param>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainNextActorsAsync<T>(ActorRequestContext nextActorRequestContext, T payload, OrchestrationOrder order, CancellationToken cancellationToken)
        {
            //put more logic on to the orchestration collection later. Now it is just get the single one
            var anotherOrderInMyActorLifeTime =
                order.ToExecutableOrder();

            await ChainNextActorsAsync(nextActorRequestContext, payload, anotherOrderInMyActorLifeTime, cancellationToken);
            //just do nothing because no order
        }

        /// <summary>
        /// Main method to chain an order take in an executable order
        /// Allow actors to directly make call to next actor via an executable order object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nextActorRequestContext"></param>
        /// <param name="payload"></param>
        /// <param name="excutableOrder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainNextActorsAsync<T>(ActorRequestContext nextActorRequestContext, T payload, ExecutableOrchestrationOrder excutableOrder, CancellationToken cancellationToken)
        {
            await ChainNextActorsAsync(nextActorRequestContext, payload, typeof(T), excutableOrder, cancellationToken);
        }

        #endregion

        #region Actor Orchestration - Non generics

        protected virtual async Task ChainNextActorsAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            // if having any request-order
            if (!Orders.IsEmpty)
            {
                //put more logic on to the orchestration collection later. Now it is just get the single one
                var anotherOrderInMyActorLifeTime =
                    Orders.GetFirstExecutableOrder();

                await ChainNextActorsAsync(nextActorRequestContext, payload, typeOfPayload, anotherOrderInMyActorLifeTime, cancellationToken);
            }
            //just do nothing because no order
        }

        /// <summary>
        /// Wrapper for orchestration-order to be converted to executable order
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nextActorRequestContext"></param>
        /// <param name="payload"></param>
        /// <param name="order"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainNextActorsAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, OrchestrationOrder order, CancellationToken cancellationToken)
        {
            //put more logic on to the orchestration collection later. Now it is just get the single one
            var anotherOrderInMyActorLifeTime =
                order.ToExecutableOrder();

            await ChainNextActorsAsync(nextActorRequestContext, payload, typeOfPayload, anotherOrderInMyActorLifeTime, cancellationToken);
            //just do nothing because no order
        }

        /// <summary>
        /// Main method to chain an order take in an executable order
        /// Allow actors to directly make call to next actor via an executable order object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="nextActorRequestContext"></param>
        /// <param name="payload"></param>
        /// <param name="excutableOrder"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainNextActorsAsync(ActorRequestContext nextActorRequestContext, object payload, Type typeOfPayload, ExecutableOrchestrationOrder excutableOrder, CancellationToken cancellationToken)
        {
            //try to complete the step
            await CompleteStepAsync(payload);

            try
            {
                await ActorClient.ChainNextActorAsync(nextActorRequestContext, payload, typeOfPayload, new ActorIdentity(excutableOrder.ActorId, excutableOrder.ActorServiceUri), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to chain next actor {excutableOrder?.ActorServiceUri} with actor id {excutableOrder?.ActorId}. Message: {ex.Message}");
            }
        }
        #endregion

        #endregion

        #region Tracking Messages
        /*
        protected IRepository<TrackingMessage, string> TrackingRepository;
        protected IEventBus FaultEventBus;

        /// <summary>
        /// Save tracking messages to storage
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="models"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task TrackMessages<T>(IEnumerable<T> models, CancellationToken cancellationToken)
        {
            if (models is IEnumerable<IMessageTrackable> trackingMessageModels)
            {
                IEnumerable<TrackingMessage> trackingMessages = trackingMessageModels.Select(x => x.ToTrackingMessage());
                foreach (var trackingMessage in trackingMessages)
                {
                    if (cancellationToken.IsCancellationRequested) return;
                    await TrackMessage(trackingMessage, cancellationToken);
                }
            }
            Logger.LogTrace($"Track {models.Count()} messages of type {typeof(T).Name} successfully");
        }

        protected virtual Task TrackMessage(TrackingMessage trackingMessage, CancellationToken cancellationToken)
        {
            trackingMessage.LastActor = CurrentActor;
            TrackingRepository.Insert(trackingMessage);
            return Task.CompletedTask;
        }

        protected virtual Task TrackMessage(TrackingMessage trackingMessage, TrackingMessageStatus status, CancellationToken cancellationToken)
        {
            trackingMessage.Status = status;
            TrackMessage(trackingMessage, cancellationToken);
            return Task.CompletedTask;
        }

        protected virtual Task TrackFailedMessage<T>(Exception exception, T model, CancellationToken cancellationToken)
        {        
            if (model is IMessageTrackable trackingMessageModel)
            {
                var trackingMessage = trackingMessageModel.ToTrackingMessage();
                trackingMessage.ExceptionMessage = $"{exception.GetType().Namespace} : {exception.Message}//// : Stacktrace {exception.StackTrace}";
                TrackMessage(trackingMessage, TrackingMessageStatus.Failed, cancellationToken);
                Logger.LogWarning($"A failed message id {trackingMessage.SourceId} of type {typeof(T).Name} has been tracked successfully");
            }           
            return Task.CompletedTask;
        }

        /// <summary>
        /// Publish a failed event to failed event bus Topic
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exception"></param>
        /// <param name="model"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual Task SendFaultMessage<T>(Exception exception, T model, CancellationToken cancellationToken) where T : IMessageTrackable
        {
            if (model is IMessageTrackable trackingMessageable)
            {
                var trackingMessage = trackingMessageable.ToTrackingMessage();
                var faultEvent = new FaultedDomainEvent(exception, trackingMessage);
                FaultEventBus.Publish(faultEvent);
            }
            return Task.CompletedTask;
        }*/
        #endregion

        #region Storage

        protected IKeyValueStorage<string> StorageService;

        protected bool IsGlobalVariableKey(string key)
        {
            if (CurrentFlowInstanceId?.Id == null) return false;
            //note: careful if flowinstanceid is the key
            return key.Contains(CurrentFlowInstanceId.Id);
        }
        /// <summary>
        /// Add flowinstance id to key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected string MakeFlowVariableKey(string key)
        {
            if (CurrentFlowInstanceId?.Id == null || CurrentFlowInstanceId?.Id == NOT_ASSIGNED_FLOW_INSTANCE)
            {
                throw new ArgumentException($"{CurrentActor} failed to make flow variable as current flowinstance id is not valid");
            }

            if (!IsGlobalVariableKey(key))
            {
                return NameCompositionResolver.GenerateFlowVariableStorageKey(key, CurrentFlowInstanceId?.Id);
            }
            return key;
        }

        protected Task<string> StoreFlowMessageAsync<T>(string key, object payload, CancellationToken cancellationToken)
        {
            var flowVariableKey = MakeFlowVariableKey(key);
            return StoreMessageAsync(flowVariableKey, payload, typeof(T), cancellationToken);
        }

        protected async Task<T> RetrieveFlowMessageAsync<T>(string key, bool isOptional = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var flowVariableKey = MakeFlowVariableKey(key);
                var dataObject = await RetrieveMessageAsync(flowVariableKey, typeof(T), isOptional, cancellationToken);
                return (T)dataObject;
            }
            catch (Exception)
            {
                if (isOptional)
                {
                    return default;
                }
                else
                    throw;
            }
        }

        protected Task<string> StoreMessageAsync<T>(string key, object payload, CancellationToken cancellationToken)
        {
            return StoreMessageAsync(key, payload, typeof(T), cancellationToken);
        }

        protected async Task<T> RetrieveMessageAsync<T>(string key, bool isOptional = false, CancellationToken cancellationToken = default)
        {
            try
            {
                var dataObject = await RetrieveMessageAsync(key, typeof(T), isOptional, cancellationToken);
                return (T)dataObject;
            }
            catch (Exception)
            {
                if (isOptional)
                {
                    return default;
                }
                else
                    throw;
            }
        }

        protected Task<string> StoreMessageAsync(string key, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            return StorageService.StoreMessageAsync(key, payload, typeOfPayload, cancellationToken);
        }

        protected Task<object> RetrieveMessageAsync(string key, Type typeOfPayload, bool isOptional, CancellationToken cancellationToken)
        {
            try
            {
                return StorageService.RetrieveMessageAsync(key, typeOfPayload, cancellationToken);
            }
            catch (Exception)
            {
                if (isOptional)
                {
                    return null;
                }
                else
                    throw;
            }
        }

        protected Task ClearMessageAsync(string key, CancellationToken cancellationToken)
        {
            return StorageService.ClearMessageAsync(key, cancellationToken);
        }

        protected Task ClearFlowMessageAsync(string key, CancellationToken cancellationToken)
        {
            var flowVariableKey = MakeFlowVariableKey(key);
            return StorageService.ClearMessageAsync(flowVariableKey, cancellationToken);
        }

        /// <summary>
        /// Schedule a task on the thread pool to delete the actor with a specific Id. Override if needed
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual void DisposeActor(ActorId actorId, Uri actorServiceUri, CancellationToken cancellationToken)
        {
            try
            {
                Task.Run(async () =>
                {
                    var serviceProxy = ActorServiceProxy.Create(actorServiceUri, actorId);
                    await serviceProxy.DeleteActorAsync(actorId, cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[{CurrentActor}-DisposeActor] Failed to dispose: {actorId.ToString()} of Service {actorServiceUri.ToString()}" + ex.Message);
            }
        }

        #endregion

        #region Flow
        //TODO: extract to Interfaces later
        protected IAsyncOrchestrationFlow<Step> AsyncFlowService;

        protected async Task CompleteStepAsync(object payload, CancellationToken cancellationToken = default)
        {
            if (AsyncFlowService != null)
            {
                try
                {
                    var step = CurrentRefStep ?? MakeStep(payload, StepStatus.InProgress);
                    await AsyncFlowService.CompleteStepAsync(CurrentActorIdentity, CurrentFlowInstanceId, step, payload, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"{CurrentActor} failed to complete the step status {StepStatus.InProgress.ToString()} with flow service. Error : {ex.Message}");
                }
            }
        }

        protected async Task CompleteFlow(object payload, CancellationToken cancellationToken = default)
        {
            if (AsyncFlowService != null)
            {
                try
                {
                    var step = CurrentRefStep ?? MakeStep(payload, StepStatus.InProgress);
                    await AsyncFlowService.CompleteFlowAsync(CurrentActorIdentity, CurrentFlowInstanceId, payload, cancellationToken);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"{CurrentActor} failed to complete the flow with flow service. Error : {ex.Message}");
                }
            }
        }


        protected Step MakeStep(object payload, StepStatus stepStatus = StepStatus.InProgress)
        {
            var step = new Step(ServiceUri.ToString(), Id.ToString(), Orders.Name);
            step.SetJsonPayload(payload);
            step.StepStatus = stepStatus;
            return step;
        }
        #endregion
    }
}