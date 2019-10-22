using Integration.Common.Flow;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Interfaces
{
    public interface IFlowStatusService : IActor
    {
        Task<bool> InitFlowAsync(ActorRequestContext actorRequestContext, FlowInstanceId flowInstanceId, CancellationToken cancellationToken);
    }
}
