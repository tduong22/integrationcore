using System.Collections.Generic;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IDataPackageReader
    {
        IDictionary<string, string> LoadDataXml(string packageName = "Data");

        string GetTemplateByQdocName(string qdocName);
    }
}