/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Comvita.Common.Actor.Flow;
using Comvita.Common.Actor.Interfaces;
using Integration.Common.Actor.BaseActor;
using Integration.Common.Flow;
using Integration.Common.Model;
using Integration.Common.Repos.Cosmos;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseActor
{

public abstract class BaseFlowActor : BaseMessagingActor, IFlowService, IStorageActor
{
    protected const string FLOW_STATE_NAME = "FLOW_STATE_NAME";

    protected const string FLOW_VARIABLE_STORAGE_STATE_NAME = "FLOW_VARIABLE_STORAGE_STATE_NAME";

    protected readonly IRepository<Integration.Common.Flow.Flow, string> FlowRepository;
    protected readonly Uri FlowStatusServiceUri;


    public const string SET_CURRENT_STEP_ACTION_NAME = "SET_CURRENT_STEP_ACTION_NAME";
    public const string CREATE_FLOW_ACTION_NAME = "CREATE_FLOW_ACTION_NAME";
    public const string ERROR_FLOW_ACTION_NAME = "ERROR_FLOW_ACTION_NAME";
    public const string COMPLETE_FLOW_ACTION_NAME = "COMPLETE_FLOW_ACTION_NAME";


    protected BaseFlowActor(ActorService actorService, ActorId actorId, IRepository<Integration.Common.Flow.Flow, string> flowRepository, Uri flowStatusService) : base(actorService, actorId)
    {
        FlowRepository = flowRepository;
        FlowStatusServiceUri = flowStatusService;
    }

    #region Early Stage of Flow Service
    public async Task CompleteFlow(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken)
    {
        Flow.Complete();
        await FlowRepository.Upsert(Flow);
        DisposeActor(new ActorId(Id.ToString()), FlowStatusServiceUri, cancellationToken);
        DisposeActor(this.Id, ServiceUri, cancellationToken);
    }
    public virtual async Task<FlowInfo> CreateFlowAsync(ActorRequestContext actorRequestContext, StartFlowRequest startFlowRequest, CancellationToken cancellationToken)
    {
        //TODO: at this moment, only init the flow and update accordingly with the already-working running instance
        Flow = new Flow.Flow(startFlowRequest.FlowInstanceId.Id, startFlowRequest.FlowInstanceId.FlowName);

        //set flow to started
        Flow.FlowStatus = FlowStatus.Started;
        Flow.StartedPayload = startFlowRequest.StartedPayload;

        //start flow status
        await InitFlowStatusAsync(startFlowRequest.FlowInstanceId, cancellationToken);

        await FlowRepository.Insert(Flow);

        return new FlowInfo()
        {
            FlowInstanceId = new FlowInstanceId()
            {
                FlowName = startFlowRequest.FlowInstanceId?.FlowName,
                Id = string.IsNullOrEmpty(startFlowRequest.FlowInstanceId.Id) ? Guid.NewGuid().ToString() : startFlowRequest.FlowInstanceId.Id
            },
            FlowStorageServiceUrl = "",
            StartDate = startFlowRequest.StartDate,
            UserCreated = startFlowRequest.UserStarted
        };
    }
    public async Task ErrorFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, string actionName, byte[] payload, Exception exception, CancellationToken cancellationToken)
    {
        Flow.FlowStatus = FlowStatus.Errored;
        /*
        var allFlowVariables = await GetAllVariables(cancellationToken);
        foreach (var variable in allFlowVariables)
        {
            Flow.AddFlowVariables(variable);
        }
await FlowRepository.Upsert(Flow);

        }
        protected override async Task OnDeactivateAsync()
        {
            await StateManager.AddOrUpdateStateAsync<Flow.Flow>(FLOW_STATE_NAME, Flow, (s, v) => Flow);
            await base.OnDeactivateAsync();
        }
        protected override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();
            if (Flow == null)
            {
                var result = await StateManager.TryGetStateAsync<Flow.Flow>(FLOW_STATE_NAME);
                if (result.HasValue)
                {
                    Flow = result.Value;
                }
            }
        }
        public Task<Step> SetCurrentStep(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, Step step, CancellationToken cancellationToken)
        {
            Flow.FlowStatus = FlowStatus.InProgress;
            CurrentRefStep = step;
            Flow.Add(step);
            return Task.FromResult(CurrentRefStep);
        }

        public virtual async Task InitFlowStatusAsync(FlowInstanceId flowInstanceId, CancellationToken cancellationToken)
        {
            if (FlowStatusServiceUri != null)
            {
                var flowStatusProxy = ActorProxy.Create<IFlowStatusService>(new ActorId(this.Id.ToString()), FlowStatusServiceUri);
                await flowStatusProxy.InitFlowAsync(new ActorRequestContext() { ManagerId = this.Id.ToString(), ManagerService = ServiceUri.ToString(), FlowInstanceId = flowInstanceId }, flowInstanceId, cancellationToken);
            }
        }

        public override Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        #endregion

        #region Later For Flow Full Manipulation
        //later use
        public Task StartFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken)
        {
            Flow.Start();
            return Task.CompletedTask;
        }

        public Task<Flow.Flow> GetFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Flow);
        }

        public Task CompleteStepAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, StepId stepId, CancellationToken cancellationToken)
        {
            var currentStep = Flow.GetCurrentStep(stepId);
            currentStep.Complete();
            return Task.CompletedTask;
        }

        public Task ErrorStepAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, StepId stepId, CancellationToken cancellationToken)
        {
            var currentStep = Flow.GetCurrentStep(stepId);
            currentStep.Error();
            return Task.CompletedTask;
        }

        protected abstract Task ComposeFlow(ActorRequestContext actorRequestContext, StartFlowRequest startFlowRequest, CancellationToken cancellationToken);

        public Task<Step> GetCurrentStep(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, ActorIdentity actorIdentity, int occurrence, CancellationToken cancellationToken)
        {
            return Task.FromResult(Flow.GetCurrentStep(actorIdentity, false, occurrence, !string.IsNullOrEmpty(actorIdentity.ActorId)));
        }

        public Task<Step> GetCurrentStepWithActionName(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, ActorIdentityWithActionName actorIdentity, int occurrence, CancellationToken cancellationToken)
        {
            return Task.FromResult(Flow.GetCurrentStep(actorIdentity, true, occurrence, !string.IsNullOrEmpty(actorIdentity.ActorId)));
        }

        #endregion

        #region Storage
        public const string SAVED_ERROR = "SAVED_ERROR";
        public const string TTL_REMINDER_NAME = "TTL_REMINDER_NAME";
        protected int MAX_TTL_IN_MINUTE = 60;

        protected string GetKeyStateName(string key)
        {
            if (key.Contains(FLOW_VARIABLE_STORAGE_STATE_NAME)) return key;
            return $"{FLOW_VARIABLE_STORAGE_STATE_NAME}_{key}";
        }
        public async Task<byte[]> RetrieveMessageAsync(ActorRequestContext actorRequestContext, string key, bool isOptional, CancellationToken cancellationToken)
        {
            try
            {
                return await StateManager.GetStateAsync<byte[]>(GetKeyStateName(key));
            }
            catch (System.Exception ex)
            {
                if (isOptional) return null;
                Logger.LogError(ex, $"Failed to retrieve the variable with key {key} from {actorRequestContext?.ManagerId}.");
                throw;
            }
        }
        public async Task<string> SaveMessageAsync(ActorRequestContext actorRequestContext, string key, byte[] payload, CancellationToken cancellationToken)
        {
            try
            {
                // save message => schedule to delete after TTL
                //await RegisterReminderAsync(GetKeyStateName(key), null, TimeSpan.FromMinutes(MAX_TTL_IN_MINUTE), TimeSpan.FromMilliseconds(-1));
                await StateManager.AddOrUpdateStateAsync(GetKeyStateName(key), payload, (k, v) => payload, cancellationToken);
                Flow.AddFlowVariables(new FlowVariable()
                {
                    Name = key,
                    Data = Encoding.Default.GetString(payload)
                });
                return key;
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Failed to store the variable with key {key}.");
                return SAVED_ERROR;
            }
        }
        protected async Task ClearMessageAsync(string key, CancellationToken cancellationToken)
        {
            try
            {
                await StateManager.TryRemoveStateAsync(GetKeyStateName(key), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"Failed to clear the variable with key {key}.");
                throw;
            }
        }
        public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(TTL_REMINDER_NAME)) // only prefix ttl reminder name
            {
                Logger.LogInformation($"Releasing resource for actor id {Id.ToString()}");
                //dispose itself
                DisposeActor(Id, ServiceUri, CancellationToken.None);
            }
            else
            {  // reminder name with key
                return ClearMessageAsync(reminderName, CancellationToken.None);
            }
            return Task.CompletedTask;
        }

        protected async Task<IEnumerable<FlowVariable>> GetAllVariables(CancellationToken cancellationToken)
        {
            var allStateNames = (await StateManager.GetStateNamesAsync()).Where(c => c.StartsWith(FLOW_VARIABLE_STORAGE_STATE_NAME));
            var result = new List<FlowVariable>();
            foreach (var stateName in allStateNames)
            {
                var value = await StateManager.GetStateAsync<byte[]>(stateName, cancellationToken);
                result.Add(new FlowVariable()
                {
                    Name = stateName,
                    Data = Encoding.Default.GetString(value)
                });
            }
            return result;
        }
        #endregion
    }
}
*/
