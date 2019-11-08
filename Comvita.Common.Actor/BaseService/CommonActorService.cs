using System;
using System.Fabric;
using Integration.Common.Logging;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseService
{
    public class CommonActorService : ActorService
    {
        protected ILogger Logger;
        public CommonActorService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo,
            ILoggerFactory loggerFactory,
            LoggerConfiguration loggerConfiguration = null,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : base(context,
            actorTypeInfo, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            Logger = loggerFactory.CreateLogger(GetType());
        }
    }
}
