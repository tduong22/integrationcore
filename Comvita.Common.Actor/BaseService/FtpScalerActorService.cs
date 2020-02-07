using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Comvita.Common.Actor.Interfaces;
using Integration.Common.Logging;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseService
{
    public class FtpScalerActorService : CommonActorService
    {
        protected IServiceConfiguration ServiceConfiguration;
        protected string ScalerActorServiceName;
        public FtpScalerActorService(StatefulServiceContext context,
            ActorTypeInformation actorTypeInfo,
            ILoggerFactory loggerFactory,
            IServiceConfiguration serviceConfiguration,
            string scalerActorServiceName,
            LoggerConfiguration loggerConfiguration = null,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null,
            ActorServiceSettings settings = null) : base(context, actorTypeInfo, loggerFactory, loggerConfiguration, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            ServiceConfiguration = serviceConfiguration;
            ScalerActorServiceName = scalerActorServiceName;
        }
        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            InitScalerActor(cancellationToken);
        }
        private void InitScalerActor(CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var data = MessagePackSerializer.Serialize(Guid.NewGuid().ToString());
                var proxy = ActorProxy.Create<IBaseMessagingActor>(new ActorId($"{Context.CodePackageActivationContext.ApplicationName}/{ScalerActorServiceName}"),
                    new Uri($"{Context.CodePackageActivationContext.ApplicationName}/{ScalerActorServiceName}"));
                proxy.ChainProcessMessageAsync(new ActorRequestContext(this.GetType().Name), data, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[InitScalerActor] Failed to init scaler actor: " + e.Message);
                throw;
            }
        }
    }
}
