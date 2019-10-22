using Integration.Common.Flow;
using Integration.Common.Model;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceFabric.Integration.Actor.Core.Flow
{
    public interface IActorFlow<TStep> : IFlow<TStep> where TStep : IStep
    {
        TStep GetCurrentStep(ActorIdentityWithActionName actorIdentityWithActionName, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false);
        TStep GetCurrentStep(ActorIdentity actorIdentity, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false);
        TStep GetCurrentStep(string currentActorServiceUri, string currentActorId, string requestedActionName, bool needToMatchActionName, int occurences, bool needToMatchSpecificActorId = false);
    }
    public interface IAsyncActorFlow<TStep> : IFlow<TStep> where TStep : IStep
    {
        Task<TStep> GetCurrentStepAsync(ActorIdentityWithActionName actorIdentityWithActionName, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false, CancellationToken cancellationToken = default);
        Task<TStep> GetCurrentStepAsync(ActorIdentity actorIdentity, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false, CancellationToken cancellationToken = default);
        Task<TStep> GetCurrentStepAsync(string currentActorServiceUri, string currentActorId, string requestedActionName, bool needToMatchActionName, int occurences, bool needToMatchSpecificActorId = false, CancellationToken cancellationToken = default);
        Task<TStep> GetNextStepAsync(TStep currentStep, CancellationToken cancellationToken = default);
        Task<TStep> GetNextStepAsync(StepId currentStepId, CancellationToken cancellationToken = default);
    }
}
