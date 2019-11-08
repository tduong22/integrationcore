using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Integration.Common.Actor.UnifiedActor;
using Integration.Common.Flow;
using Integration.Common.Logging;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.BaseService
{
    public class DefaultFtpActionService : CommonActorService
    {
        private readonly string _scalerActorServiceName;
        private readonly string _scalerActorId;

        public DefaultFtpActionService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo,
            ILoggerFactory loggerFactory,
            string scalerActorServiceName,
            string scalerActorId,
            LoggerConfiguration loggerConfiguration = null,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : base(context,
            actorTypeInfo, loggerFactory, loggerConfiguration, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            _scalerActorServiceName = scalerActorServiceName;
            _scalerActorId = scalerActorId;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            await ActionInvoker.Invoke<IDefaultFtpScalerAction>(c=>c.InvokeFtpScaler(),
                 new ActorRequestContext($"{this.GetType().Name}", nameof(IDefaultFtpScalerAction), Guid.NewGuid().ToString(), FlowInstanceId.NewFlowInstanceId),
                    new ExecutableOrchestrationOrder()
                    {
                        ActorId = _scalerActorId,
                        ActorServiceUri = $"{FabricRuntime.GetActivationContext().ApplicationName}/{_scalerActorServiceName}"
                    }, cancellationToken);
        }
    }
}
