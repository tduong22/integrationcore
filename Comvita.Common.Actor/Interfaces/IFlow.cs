using System;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Flow;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IFlowService : IActor
    {
        #region Flow
        Task<Integration.Common.Flow.Flow> GetFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken);
        Task<FlowInfo> CreateFlowAsync(ActorRequestContext actorRequestContext, StartFlowRequest startFlowRequest, CancellationToken cancellationToken);
        Task StartFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken);
        Task ErrorFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, string actionName, byte[] payload, Exception exception, CancellationToken cancellationToken);
        Task CompleteFlow(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken);
        #endregion

        #region steps
        Task<Step> SetCurrentStep(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, Step step, CancellationToken cancellationToken);
        Task CompleteStepAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, StepId stepId, CancellationToken cancellationToken); 
        Task ErrorStepAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, StepId stepId, CancellationToken cancellationToken);
        Task<Step> GetCurrentStep(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, ActorIdentity actorIdentity, int occurrence, CancellationToken cancellationToken);
        Task<Step> GetCurrentStepWithActionName(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, ActorIdentityWithActionName actorIdentity, int occurrence, CancellationToken cancellationToken);
        #endregion
    }
}
