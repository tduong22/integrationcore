using System.Fabric;

namespace ServiceFabric.Integration.Actor.Core.Configuration
{
    public static class ConfigurationHelper
    {
        public static string GetConfigValue(string section, string configName)
        {
            return FabricRuntime.GetActivationContext()?
                   .GetConfigurationPackageObject("Config")?
                   .Settings.Sections[section]?
                   .Parameters[configName]?.Value;
        }
    }
}
