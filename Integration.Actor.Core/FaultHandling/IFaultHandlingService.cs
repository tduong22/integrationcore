using System;
using System.Threading;
using System.Threading.Tasks;
using ServiceFabric.Integration.Actor.Core.Models;

namespace ServiceFabric.Integration.Actor.Core.FaultHandling
{
    public interface IFaultHandlingService
    {
        Task HandleFaultAsync(string actionName, object payload, Exception exception, ActorExecutionContext actorExecutionContext, CancellationToken cancellationToken);
    }
}
