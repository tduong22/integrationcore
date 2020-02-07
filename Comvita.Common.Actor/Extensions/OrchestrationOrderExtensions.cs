using Integration.Common.Model;
using System;

namespace Comvita.Common.Actor.Extensions
{
    public static class OrchestrationOrderExtensions
    {
        public static ExecutableOrchestrationOrder ToExecutableOrder(this OrchestrationOrder orchestrationOrder) => new ExecutableOrchestrationOrder()
        {
            ActorId = orchestrationOrder.ActorId ?? Guid.NewGuid().ToString(),
            ActorServiceUri = (orchestrationOrder.ActorServiceUri == null) ? null : string.Copy(orchestrationOrder.ActorServiceUri.ToString()),
            Condition = orchestrationOrder.Condition == null ? null : string.Copy(orchestrationOrder.Condition)
        };
    }
}
