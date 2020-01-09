using System.Fabric;

namespace Integration.Common.Actor.Utilities
{
    public class ServiceFabricUtility
    {
        public static string GenerateFullActorServiceName(string actorServiceName, string applicationName = null)
        {
            var resolvedApplicationName = string.IsNullOrEmpty(applicationName) ? FabricRuntime.GetActivationContext().ApplicationName : applicationName;
            return $"{resolvedApplicationName}/{actorServiceName}";
        }
    }
}
