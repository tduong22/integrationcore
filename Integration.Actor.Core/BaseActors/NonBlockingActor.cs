using Integration.Common.Actor.Helpers;
using Integration.Common.Actor.Interface;
using Integration.Common.Actor.Model;
using Integration.Common.Actor.Persistences;
using Integration.Common.Actor.UnifiedActor;
using Integration.Common.Actor.Utilities;
using Integration.Common.Exceptions;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.Integration.Actor.Core.FaultHandling;
using ServiceFabric.Integration.Actor.Core.Loggings;
using ServiceFabric.Integration.Actor.Core.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.BaseActor
{
    public abstract class NonBlockingActor : BaseMessagingActor, IRemindable
    {
        #region Actor Members
        private readonly TimeSpan _dueTime = TimeSpan.FromMilliseconds(5);
        private readonly TimeSpan _periodTimeInfinite = TimeSpan.FromMilliseconds(-1);

        //use default action name for any single-purpose actor
        protected readonly string DefaultActionName = "DEFAULT_ACTION_NAME";

        protected readonly IActorRequestPersistence ActorRequestPersistence;

        protected const string NOT_VALID_PARSING = "NOT_VALID_FOR_PARSING";
        protected const string REMINDER_NAME = "ReminderName";

        protected IFaultHandlingService _faultHandlingService;

        #endregion

        protected NonBlockingActor(ActorService actorService, ActorId actorId,
                                   IActorRequestPersistence actorRequestPersistence,
                                   IBinaryMessageSerializer binaryMessageSerializer,
                                   IActorClient actorClient,
                                   IKeyValueStorage<string> storage, ILogger logger) : base(actorService,
                                                                                            actorId,
                                                                                            binaryMessageSerializer,
                                                                                            actorClient,
                                                                                            storage,
                                                                                            logger)
        {
            ActorRequestPersistence = actorRequestPersistence;
            ActorRequestPersistence.SetActorStateManager(StateManager);
        }

        /// <summary>
        /// Chain the next request using (MessagePack) binary serialized data.
        /// TPayload is the type that the received actor will try to deserialize payload to.
        /// </summary>
        /// <typeparam name="TPayload"></typeparam>
        /// <param name="requestContext"></param>
        /// <param name="payload"></param>
        /// <param name="actionName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainRequestAsync<TPayload>(ActorRequestContext requestContext, byte[] payload, string actionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {

            var isRequestValid = ValidateRequestAsync(requestContext, cancellationToken);
            if (isRequestValid.Result)
            {
                try
                {
                    //serialize back to object
                    var deserializedPayload = DeserializePayload<TPayload>(payload);

#pragma warning disable 618
                    await ChainRequestAsync(requestContext, deserializedPayload, actionName, cancellationToken);
#pragma warning restore 618
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[{CurrentActor}-ChainRequestAsync]Failed to deserialize of type {typeof(TPayload)}: " + ex.Message);
                    throw;
                }
            }
            else
            {
                var exception = new ActorRequestInvalidException(
                    $"Actor Request {requestContext.RequestId} with action name {requestContext.ActionName} from {requestContext.ManagerId} is failed to validate and will not be processed by actor {Id} of the service {ActorService.ActorTypeInformation.ServiceName}. The exception will be propagated to the caller and possibly result in a retry.");
                Logger.LogError(exception, exception.Message);
                throw exception;
            }
        }

        /// <summary>
        /// Non generic version of ChainRequestAsync.
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="payload"></param>
        /// <param name="typeOfPayload"></param>
        /// <param name="actionName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainRequestAsync(ActorRequestContext requestContext, byte[] payload,
            Type typeOfPayload, string actionName = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            var isRequestValid = ValidateRequestAsync(requestContext, cancellationToken);
            if (isRequestValid.Result)
            {
                try
                {

#pragma warning disable 618
                    await ChainRequestAsyncNonGeneric(requestContext, payload, typeOfPayload, actionName, cancellationToken);
#pragma warning restore 618
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, $"[{CurrentActor}-ChainRequestAsync]Failed to deserialize of type {typeOfPayload.Name}: " + ex.Message);
                    throw;
                }
            }
            else
            {
                var exception = new ActorRequestInvalidException(
                    $"Actor Request {requestContext.RequestId} with action name {requestContext.ActionName} from {requestContext.ManagerId} is failed to validate and will not be processed by actor {Id} of the service {ActorService.ActorTypeInformation.ServiceName}. The exception will be propagated to the caller and possibly result in a retry.");
                Logger.LogError(exception, exception.Message);
                throw exception;
            }
        }

        [Obsolete("This method will be deprecated and unsupported. Please change the signature to ChainRequestAsync with the byte[] payload instead.")]
        protected virtual async Task ChainRequestAsync<TPayload>(ActorRequestContext requestContext, TPayload payload,
            string actionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                //any specify reminder name? if not, use default
                var currentActionName = actionName ?? requestContext.ActionName;
                var currentReminderName = NameCompositionResolver.GenerateReminderName(currentActionName, requestContext.RequestId);

                //persist request to state
                await ActorRequestPersistence.SaveRequest(currentActionName, requestContext, payload, cancellationToken);
                // register reminder to process the request later
                await RegisterReminderAsync(currentReminderName, null,
                    _dueTime, _periodTimeInfinite);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[{CurrentActor}-ChainRequestAsync] Failed to chain request of type {typeof(TPayload)}: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Non generic version of ChainRequestAsync
        /// </summary>
        /// <param name="requestContext"></param>
        /// <param name="payload"></param>
        /// <param name="typeOfPayload"></param>
        /// <param name="actionName"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected virtual async Task ChainRequestAsyncNonGeneric(ActorRequestContext requestContext, byte[] payload, Type typeOfPayload,
            string actionName = null, CancellationToken cancellationToken = default(CancellationToken))
        {

            try
            {
                //any specify reminder name? if not, use default
                var currentActionName = actionName ?? requestContext.ActionName;
                var currentReminderName = NameCompositionResolver.GenerateReminderName(currentActionName, requestContext.RequestId);

                //persist request to state
                await ActorRequestPersistence.SaveRequest(currentActionName, requestContext, typeOfPayload, payload, cancellationToken);
                // register reminder to process the request later
                await RegisterReminderAsync(currentReminderName, null,
                    _dueTime, _periodTimeInfinite);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[{CurrentActor}-ChainRequestAsync] Failed to chain request of type {typeOfPayload.Name}: " + e.Message);
                throw;
            }
        }

        /// <summary>
        /// Process the request with the payload saved of the actionName.
        /// </summary>
        /// <param name="actionName"></param>
        /// <param name="payload"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected abstract Task<MessageObjectResult> InternalProcessAsync(string actionName, object payload, CancellationToken cancellationToken);

        #region Actor Reminders
        /// <summary>
        /// Reminder will tick very quickly after the actor saves request to state.
        /// Reminder will load up states based on reminder names and pass to InternalProcessAsync
        /// </summary>
        /// <param name="reminderName"></param>
        /// <param name="state"></param>
        /// <param name="dueTime"></param>
        /// <param name="period"></param>
        /// <returns></returns>
        public virtual async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (IsRequestPersistenceReminder(reminderName))
            {
                object payload = null;
                CancellationToken cancellationToken = CancellationToken.None;
                var actionName = NameCompositionResolver.ExtractActionNameFromReminderName(reminderName);

                try
                {
                    var currentRequestContextId =
                        NameCompositionResolver.ExtractRequestContextIdFromReminderName(reminderName);

                    //assign memory values, can be done with get/set
                    CurrentRequestContext = await
                        ActorRequestPersistence.RetrieveRequestContextAsync(actionName, currentRequestContextId,
                            cancellationToken);

                    payload =
                        await ActorRequestPersistence.RetrieveRequestPayloadAsync<object>(actionName, currentRequestContextId,
                            cancellationToken);

                    //{current actor} only resolved fully from here, when current request context is available
                    var serviceName = this.ActorService.Context.ServiceName;

                    var actorExecutionContext = new ActorExecutionContext()
                    {
                        ServiceUri = serviceName.ToString(),
                        Payload = null,
                        MethodName = "NOT_YET_PARSED",
                        ActionName = NameCompositionResolver.ExtractActionNameFromReminderName(REMINDER_NAME),
                        ActorName = CurrentActor,
                        ReminderName = reminderName,
                        OperationId = CurrentFlowInstanceId.Id,
                        FlowName = CurrentFlowInstanceId.FlowName,
                        ApplicationName = ApplicationName,
                        Resendable = false,
                        SourceSystem = CurrentFlowInstanceId.SourceSystem,
                        Entity = CurrentFlowInstanceId.Entity,
                        EntityId = CurrentFlowInstanceId.EntityId,
                        ServiceFabricTraceId = ActorService.Context.TraceId
                    };
                    var dict = LoggingUtilities.CreateLoggingDictionary(actorExecutionContext);

                    LoggingUtilities.LogPayload(Logger, payload, actionName, dict, null, reminderName);

                    //for non-generic cases
                    var typeOfPayload = await ActorRequestPersistence.RetrieveRequestPayloadTypeAsync(actionName, currentRequestContextId,
                           cancellationToken);

                    //TODO: current implementation supports generic/non-generic payload, consider using byte[] for all purpose
                    if (typeOfPayload != null)
                    {
                        payload = DeserializePayload((byte[])payload, typeOfPayload);
                    }

                    //main process logic
                    if (await ValidateDataAsync(actionName, payload, cancellationToken))
                    {
                        var result = await InternalProcessAsync(actionName, payload, cancellationToken);
                        await OnSuccessAsync(actionName, payload, result, cancellationToken);
                    }
                    else
                        await OnFailedAsync(actionName, payload,
                            new ActorMessageValidationException(
                                $"{CurrentActor} failed to validate its message of request {currentRequestContextId}"), cancellationToken);
                }
                catch (Exception e)
                {
                    await OnFailedAsync(actionName, payload, e, cancellationToken);
                    throw;
                }
                finally
                {
                    await ActorRequestPersistence.RemoveStateDataForRequestIdAsync(actionName, CurrentRequestContext.RequestId, cancellationToken);
                    //unregister reminder after process
                    await UnregisterReminderAsync(GetReminder(reminderName));
                }
            }
        }

        #endregion

        #region Virtual Methods that should be implemented        

        protected virtual async Task OnFailedAsync(string actionName, object payload, Exception exception, CancellationToken cancellationToken)
        {
            //var dict = CreateLoggingDictionary("UNABLE_TO_PARSE", string.Empty);
            var actorExecutionContext = new ActorExecutionContext()
            {
                ServiceUri = this.ActorService.Context.ServiceName.ToString(),
                Payload = null,
                MethodName = "NOT_YET_PARSED",
                ActionName = actionName,
                ActorName = CurrentActor,
                OperationId = CurrentFlowInstanceId.Id,
                FlowName = CurrentFlowInstanceId.FlowName,
                ApplicationName = ApplicationName,
                Resendable = false,
                SourceSystem = CurrentFlowInstanceId.SourceSystem,
                Entity = CurrentFlowInstanceId.Entity,
                EntityId = CurrentFlowInstanceId.EntityId,
                ServiceFabricTraceId = ActorService.Context.TraceId
            };
            try
            {
                if (payload is SerializableMethodInfo serializableMethodInfo)
                {
                    foreach (var ar in serializableMethodInfo.Arguments)
                    {
                        actorExecutionContext.CustomRequestInfo.Add(ar.ArgumentName, BinaryMessageSerializer.ToJson(ar.Value));
                    }
                    actorExecutionContext.MethodName = serializableMethodInfo.MethodName;
                }
            }
            catch (Exception ex)
            {
                //failed to parse payload for logging
                actorExecutionContext.Payload = "UNABLE_TO_PARSE" + $" {ex.Message}";
            }

            if (_faultHandlingService != null)
            {
                await _faultHandlingService.HandleFaultAsync(actionName, payload, exception, actorExecutionContext, cancellationToken);
            }

            
            //return Task.CompletedTask;
        }
        /// <summary>
        /// Schedule a task on the thread pool to delete the actor if-self. Override if needed
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual void DisposeSelf(CancellationToken cancellationToken)
        {
            try
            {
                var serviceUri = ActorService.Context.ServiceName;
                var actorId = Id.ToString();
                Task.Run(async () =>
                {
                    var deletingActorId = new ActorId(actorId);
                    var serviceProxy = ActorServiceProxy.Create(serviceUri, deletingActorId);
                    await serviceProxy.DeleteActorAsync
                    (deletingActorId, cancellationToken);
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[{CurrentActor}-DisposeSelf] Failed to dispose: " + ex.Message);
            }
        }

        protected virtual Task OnSuccessAsync(string actionName, object payload, MessageObjectResult result,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            return Task.CompletedTask;
        }

        #endregion

        public bool IsRequestPersistenceReminder(string reminderName)
        {
            return NameCompositionResolver.IsValidRequestPersistenceForNonBlocking(reminderName);
        }

    }
}
