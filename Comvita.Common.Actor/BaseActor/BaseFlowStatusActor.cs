using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comvita.Common.Actor.Interfaces;
using Integration.Common.Actor.BaseActor;
using Integration.Common.Flow;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseActor
{
    public abstract class BaseFlowStatusActor : BaseMessagingActor, IFlowStatusService
    {
        protected FlowStatus FlowStatus;
        protected string FlowServiceUri;
        protected FlowInstanceId FlowInstanceId;

        protected List<IConditionalChecking> ConditionalCheckings = new List<IConditionalChecking>();

        protected BaseFlowStatusActor(ActorService actorService, ActorId actorId,
                                      Integration.Common.Interface.IBinaryMessageSerializer binaryMessageSerializer,
                                      Integration.Common.Actor.Interface.IActorClient actorClient,
                                      Integration.Common.Interface.IKeyValueStorage<string> storage, ILogger logger) : base(actorService, actorId, binaryMessageSerializer, actorClient, storage, logger)
        {
            FlowStatus = FlowStatus.Started;
        }

        public virtual Task<bool> InitFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken)
        {
            FlowInstanceId = flowInstanceId;
            FlowServiceUri = actorRequestContext.ManagerService;
            return FlowInitialize(actorRequestContext, flowInstanceId, cancellationToken);
        }

        protected abstract Task<bool> FlowInitialize(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken);

        public virtual async Task CompleteFlowAsync(CancellationToken cancellationToken)
        {

            var flowProxy = ActorProxy.Create<IFlowService>(this.Id, new Uri(FlowServiceUri));
            await flowProxy.CompleteFlow(new ActorRequestContext(this.Id.ToString(), Guid.NewGuid().ToString()), FlowInstanceId, cancellationToken);
        }
        public virtual async Task ErrorFlowAsync(CancellationToken cancellationToken)
        {
            var flowProxy = ActorProxy.Create<IFlowService>(new ActorId(FlowInstanceId.Id), new Uri(FlowServiceUri));

            await flowProxy.ErrorFlowAsync(new ActorRequestContext(this.Id.ToString(), Guid.NewGuid().ToString()), FlowInstanceId, null, null, null, cancellationToken);
        }

        public override Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
