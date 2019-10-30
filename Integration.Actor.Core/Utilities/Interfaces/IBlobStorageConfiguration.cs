using System.Collections.Generic;
using System.Threading.Tasks;

namespace Integration.Common.Utility.Interfaces
{
    public interface IBlobStorageConfiguration
    {
        Task<IEnumerable<T>> GetConfigsFromBlobFile<T>(string blobConnectionString,
            string blobContainerName, string blobConfigFileName, string sectionName);

        Task<string> GetContentFromConfigurationFile(string blobConnectionString, string blobContainerName,
            string blobConfigFileName);

    }
}
