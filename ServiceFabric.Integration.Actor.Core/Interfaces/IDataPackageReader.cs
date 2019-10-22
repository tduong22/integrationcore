using System.Collections.Generic;

namespace Integration.Common.Actor.Interface
{
    public interface IDataPackageReader
    {
        IDictionary<string, string> LoadDataXml(string packageName = "Data");

        string GetTemplateByQdocName(string qdocName);
    }
}