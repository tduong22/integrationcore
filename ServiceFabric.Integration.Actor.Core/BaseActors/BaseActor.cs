using Integration.Common.Actor.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Integration.Common.Actor.BaseActor
{
    public abstract class BaseActor : Microsoft.ServiceFabric.Actors.Runtime.Actor
    {
        protected ILogger Logger;

        protected BaseActor(ActorService actorService, ActorId actorId) : base(actorService, actorId)
        {
            var loggerFactory = BaseDependencyResolver.ResolveLoggerFactory();
            //GetType() return Castle.Proxies type so move up one base level to get correct actor type
            Logger = loggerFactory.CreateLogger(GetType().BaseType);
        }
    }
}
