using Integration.Common.Flow;
using Integration.Common.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Interface
{
    public interface IAsyncOrchestrationFlow<TStep>
    {
        Task CreateFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, StartFlowRequest startFlowRequest, CancellationToken cancellationToken);
        Task CompleteStepAsync(ActorIdentity currentActor, FlowInstanceId flowId, TStep step, object payload, CancellationToken cancellationToken);
        Task ErrorStepAsync(ActorIdentity currentActor, FlowInstanceId flowId, TStep step, object payload, Exception exception, CancellationToken cancellationToken);
        Task CompleteFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, object payload, CancellationToken cancellationToken);
        Task ErrorFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, object payload, Exception exception, CancellationToken cancellationToken);
    }
}
