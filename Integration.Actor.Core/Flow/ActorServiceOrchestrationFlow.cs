using Integration.Common.Interface;
using Integration.Common.Model;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Flow
{
    public class ActorServiceOrchestrationFlow : IAsyncOrchestrationFlow<Step>
    {
        private FlowServiceInfo _flowServiceInfo;
        public ActorServiceOrchestrationFlow(FlowServiceInfo flowServiceInfo)
        {
            _flowServiceInfo = flowServiceInfo;
        }

        public async Task CompleteFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //var flowProxy = ActorProxy.Create<IFlowService>(new ActorId(flowId.Id), _flowServiceInfo.ServiceUri);
            // await flowProxy.CompleteFlow(new ActorRequestContext(currentActor.ActorId, BaseFlowActor.COMPLETE_FLOW_ACTION_NAME, Guid.NewGuid().ToString()), flowId, cancellationToken);
        }

        public async Task CompleteStepAsync(ActorIdentity currentActor, FlowInstanceId flowId, Step step, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            // var flowProxy = ActorProxy.Create<IFlowService>(new ActorId(flowId.Id), _flowServiceInfo.ServiceUri);
            // await flowProxy.SetCurrentStep(new ActorRequestContext(currentActor.ActorId, BaseFlowActor.SET_CURRENT_STEP_ACTION_NAME, Guid.NewGuid().ToString()), flowId, step, cancellationToken);
        }

        public async Task CreateFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, StartFlowRequest startFlowRequest, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            // var flowProxy = ActorProxy.Create<IFlowService>(new ActorId(flowId.Id), _flowServiceInfo.ServiceUri);
            //     await flowProxy.CreateFlowAsync(new ActorRequestContext(currentActor.ActorId, BaseFlowActor.CREATE_FLOW_ACTION_NAME, Guid.NewGuid().ToString()), startFlowRequest, cancellationToken);
        }

        public async Task ErrorFlowAsync(ActorIdentity currentActor, FlowInstanceId flowId, object payload, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
            //  var flowProxy = ActorProxy.Create<IFlowService>(new ActorId(flowId.Id), _flowServiceInfo.ServiceUri);
            // await flowProxy.ErrorFlowAsync(new ActorRequestContext(currentActor.ActorId, BaseFlowActor.ERROR_FLOW_ACTION_NAME, Guid.NewGuid().ToString()), flowId, BaseFlowActor.ERROR_FLOW_ACTION_NAME, null, exception, cancellationToken);
        }

        public Task ErrorStepAsync(ActorIdentity currentActor, FlowInstanceId flowId, Step step, object payload, Exception exception, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
