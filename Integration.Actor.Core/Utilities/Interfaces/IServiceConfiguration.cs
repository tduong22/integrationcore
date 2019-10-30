using System.Collections.Generic;

namespace Integration.Common.Utility.Interfaces
{
    public interface IServiceConfiguration
    {
        string GetValue(string package, string section, string param);

        IDictionary<string, string> GetConfigSection(string sectionName);
    }
}
