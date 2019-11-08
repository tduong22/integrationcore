namespace Comvita.Common.Actor.Extensions
{
    /*
    public static class NonBlockingActorExtensions
    {
        public static OrchestrationOrder GetGenericDetourUri(this NonBlockingActor actor)
        {
            return new OrchestrationOrder(DetourConstants.DEFAULT_GENERIC_DETOUR_ACTOR);
        }

        public static Task InvokeDetour(this NonBlockingActor actor, string detourKey, string defaultNextActorUri,
            string nextActionName, object nextActorPayload, CancellationToken cancellationToken, string nextActorId = null)
        {
            var detourPayload = GenerateDetourPayload(detourKey, defaultNextActorUri, nextActionName,
                nextActorPayload, nextActorId);

            return actor.InvokeChainNextActorsAsync(detourPayload.NextActorActionName, detourPayload, cancellationToken);
        }

        private static DetourPayload GenerateDetourPayload(string detourKey, string defaultNextActorUri,
            string nextActorActionName, object nextActorPayload, string nextActorId = null)
        {
            return new DetourPayload(detourKey, defaultNextActorUri, nextActorActionName,
                nextActorPayload, nextActorId);
        }
    }*/
}
