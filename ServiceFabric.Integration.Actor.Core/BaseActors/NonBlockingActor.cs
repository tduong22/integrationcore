using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.Helpers;
using Integration.Common.Actor.Model;
using Integration.Common.Actor.Persistences;
using Integration.Common.Actor.Utilities;
using Integration.Common.Exceptions;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json;

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
        protected bool Resendable = false;

        #endregion

        protected NonBlockingActor(ActorService actorService, ActorId actorId, IActorRequestPersistence actorRequestPersistence) : base(actorService, actorId)
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

                    string payloadStr = NOT_VALID_PARSING;

                    if (Resendable)
                    {
                        //only parsed data if resendable. Byte[] data could not be seen via Application Insight as well so no need to parse
                        payloadStr = payload is byte[] bytes
                            ? BitConverter.ToString(bytes)
                            : BitConverter.ToString(SerializePayload(payload));
                    }

                    var dict = new Dictionary<string, object>{
                    {"ServiceUri", serviceName},
                    {"Payload", payloadStr},
                    {"ActionName", actionName},
                    {"Actor", CurrentActor},
                    {"ReminderName", reminderName},
                    {"OperationId", CurrentFlowInstanceId.Id},
                    {"FlowName", CurrentFlowInstanceId.FlowName},
                    {"ApplicationName", ApplicationName},
                    {"Resendable", Resendable}
                };

                    LogPayload(payload, actionName, dict, null, reminderName);

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
                finally {
                    await ActorRequestPersistence.RemoveStateDataForRequestIdAsync(actionName, CurrentRequestContext.RequestId, cancellationToken);
                    //unregister reminder after process
                    await UnregisterReminderAsync(GetReminder(reminderName));
                }
            }
        }

        private void LogPayload(object payload, string actionName, Dictionary<string, object> dictionary, Exception exception = null, string reminderName = null)
        {

            if (exception != null)
            {
                Logger.Log(LogLevel.Error, new EventId(9999),
                    dictionary
                    , exception,
                    (s, ex) =>
                        $"[OnFailedAsync] {s["Actor"]} failed to process message by {s["ActionName"]}. Message: {ex.Message}");
            }
            else
            {
                Logger.Log(LogLevel.Information, new EventId(9999),
                    dictionary
                    , exception,
                    (s, ex) =>
                        $"{s["Actor"]} invokes Internal Process with Action Name {s["ActionName"]} and reminder name: {s["ReminderName"]}");
            }


        }

        #endregion

        #region Virtual Methods that should be implemented        

        protected virtual async Task OnFailedAsync(string actionName, object payload, Exception exception, CancellationToken cancellationToken)
        {
            /*
            string payloadStr = NOT_VALID_PARSING;
            if (Resendable)
            {
                //only parsed data if resendable. Byte[] data could not be seen via Application Insight as well so no need to parse
                payloadStr = payload is byte[] bytes ? BitConverter.ToString(bytes) : BitConverter.ToString(SerializePayload(payload));
            }
            var dict = new Dictionary<string, object>
            {
                {"ServiceUri", ServiceUri},
                {"Payload", payloadStr},
                {"ActionName", actionName},
                {"Actor", CurrentActor},
                {"OperationId", CurrentFlowInstanceId.Id},
                {"FlowName", CurrentFlowInstanceId.FlowName},
                {"ApplicationName", ApplicationName},
                {"Resendable", Resendable}
            };
            var errorHandling = new ErrorHandling(ServiceUri.ToString(), payloadStr, actionName, CurrentActor,
                "ReminderName", CurrentFlowInstanceId.Id, new CorrelationId(CurrentFlowInstanceId.ToString()), CurrentFlowInstanceId.FlowName, ApplicationName);

            //if message failed by validation
            if (exception is ActorMessageValidationException)
            {
                Logger.LogError(exception, $"{CurrentActor} failed to validate the message by {actionName}");
            }
            else
            {
                LogPayload(payload, actionName, dict, exception);
            }

            /*
            try
            {
                var proxy = ActorProxy.Create<IBaseMessagingActor>(new ActorId(Guid.NewGuid().ToString()),
                        new Uri(InfrastructureConstants.ErrorEventBusSenderActorServiceUri));
                await proxy.ChainProcessMessageAsync(DefaultNextActorRequestContext,
                    SerializePayload(errorHandling), CancellationToken.None);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[OnFailedAsync] {CurrentActor} failed to chain error handling process message of the action name {actionName}. Message: {ex.Message}");
            }
            */
            //Clean up state even in failed requests

            var dict = new Dictionary<string, object>
            {
                {"ServiceUri", ServiceUri},
                {"Payload", JsonConvert.SerializeObject(payload)},
                {"ActionName", actionName},
                {"Actor", CurrentActor},
                {"OperationId", CurrentFlowInstanceId.Id},
                {"FlowName", CurrentFlowInstanceId.FlowName},
                {"ApplicationName", ApplicationName},
                {"Resendable", Resendable}
            };

            if (exception is ActorMessageValidationException)
            {
                Logger.LogError(exception, $"{CurrentActor} failed to validate the message by {actionName}");
            }
            else
            {
                LogPayload(payload, actionName, dict, exception);
            }

            var requestId = CurrentRequestContext.RequestId;
            Logger.LogInformation($"{CurrentActor} OnFailed - Cleaning up state for {actionName} - {requestId}");
            await ActorRequestPersistence.RemoveStateDataForRequestIdAsync(actionName, requestId, CancellationToken.None);
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
            //await ActorRequestPersistence.RemoveStateDataForRequestIdAsync(actionName, CurrentRequestContext.RequestId, cancellationToken);

            //if we need to quickly re-claim resource, uncomment this part otherwise just leave the actor for GC
            //for now we consider that this cause more harm than good so commented
            //DisposeSelf(cancellationToken);
            return Task.CompletedTask;

            /*
            var requestContexts = await ActorRequestPersistence.RetrieveRequestContextsAsync(cancellationToken);
            if (requestContexts == null || !requestContexts.Any())
            {
                DisposeSelf(cancellationToken);
            }*/
            
        }

        #endregion

        public Task InvokeChainNextActorsAsync<T>(string actionName, T payload, CancellationToken cancellationToken)
        {
            CurrentRequestContext.ActionName = actionName;
            return ChainNextActorsAsync<T>(CurrentRequestContext, payload, cancellationToken);
        }

        public bool IsRequestPersistenceReminder(string reminderName)
        {
            return NameCompositionResolver.IsValidRequestPersistenceForNonBlocking(reminderName);
        }
    }
}
