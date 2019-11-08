using Comvita.Common.Actor.Interfaces;
using System;
using System.Collections.Generic;
using System.Fabric;
using System.IO;

namespace Comvita.Common.Actor.Utilities
{
    public class DataPackageReader : IDataPackageReader
    {
        private readonly IDictionary<string, string> _requestTemplates;

        public DataPackageReader()
        {
            _requestTemplates = LoadDataXml();
        }

        public IDictionary<string, string> LoadDataXml(string packageName = "Data")
        {
            var result = new Dictionary<string, string>();
            var dataPackage = FabricRuntime.GetActivationContext()?.GetDataPackageObject(packageName);
            if (dataPackage == null) return result;

            foreach (var file in Directory.EnumerateFiles(dataPackage.Path, "*.json"))
            {
                try
                {
                    var jsonStr = File.ReadAllText(file);
                    //var reqTemp = JsonConvert.DeserializeObject<LegacyRequestMessage>(jsonStr);
                    result.Add(Path.GetFileNameWithoutExtension(file), jsonStr);
                }
                catch (Exception e)
                {
                    throw;
                }
            }

            return result;
        }

        public string GetTemplateByQdocName(string requestName)
        {
            if (_requestTemplates.ContainsKey(requestName))
            {
                return _requestTemplates[requestName];
            }

            throw new FileNotFoundException($"Cannot load request name {requestName}");
        }
    }
}