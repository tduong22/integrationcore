using Integration.Common.Utility.Interfaces;
using System.Collections.Generic;
using System.Fabric;

namespace Integration.Common.Utility
{
    public class ServiceConfiguration : IServiceConfiguration
    {
        private const string Default_Package_Name = "Config";

        public string GetValue(string package, string section, string param)
        {
            return FabricRuntime.GetActivationContext()?
                .GetConfigurationPackageObject(package)?
                .Settings.Sections[section]?
                .Parameters[param]?.Value;
        }

        public IDictionary<string, string> GetConfigSection(string sectionName)
        {
            var configs = new Dictionary<string, string>();
            var section = FabricRuntime.GetActivationContext()?
                .GetConfigurationPackageObject(Default_Package_Name)?
                .Settings.Sections[sectionName];

            foreach (var configurationProperty in section.Parameters)
            {
                configs.Add(configurationProperty.Name, configurationProperty.Value);
            }

            return configs;
        }
    }
}
