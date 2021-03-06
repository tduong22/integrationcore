﻿using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Integration.Common.Actor.BaseActor
{
    public abstract class BaseActor : Microsoft.ServiceFabric.Actors.Runtime.Actor
    {
        protected ILogger Logger;

        protected BaseActor(ActorService actorService, ActorId actorId, ILogger logger) : base(actorService, actorId)
        {
            Logger = logger;
        }
    }
}
