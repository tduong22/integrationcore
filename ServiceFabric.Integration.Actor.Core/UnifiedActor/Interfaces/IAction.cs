using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.Model;
using Integration.Common.Model;

namespace Integration.Common.Actor.UnifiedActor
{
    public interface IAction
    {
        Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload,
           CancellationToken cancellationToken);
        Task<MessageObjectResult> InternalProcessAsync(string actionName, object payload, CancellationToken cancellationToken);
        void SetActor(UnifiedActor unifiedActor);
        Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken);
    }
}