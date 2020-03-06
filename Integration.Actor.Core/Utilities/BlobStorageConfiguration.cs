using Integration.Common.Utility.Interfaces;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Integration.Common.Utility
{
    public class BlobStorageConfiguration : IBlobStorageConfiguration
    {
        private static CloudStorageAccount _storageAccount;
        private static CloudBlobContainer _cloudBlobContainer;
        public async Task<IEnumerable<T>> GetConfigsFromBlobFile<T>(string blobConnectionString, string blobContainerName, string blobConfigFileName,
            string sectionName)
        {
            List<T> configList = new List<T>();
            string configs = string.Empty;
            if (!CloudStorageAccount.TryParse(blobConnectionString, out _storageAccount))
            {
                return configList;
            }

            var cloudBlobClient = _storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = cloudBlobClient.GetContainerReference(blobContainerName);
            var blob = _cloudBlobContainer.GetBlobReference(blobConfigFileName);
            if (await blob.ExistsAsync())
            {
                using (var reader = new StreamReader(await blob.OpenReadAsync()))
                {
                    configs = reader.ReadToEnd();
                }
            }
            var configObj = JObject.Parse(configs);
            if (!((IDictionary<string, JToken>)configObj).ContainsKey(sectionName))
                return configList;

            configList = JsonConvert.DeserializeObject<List<T>>(configObj[sectionName].ToString());
            return configList;
        }


        public async Task<Dictionary<string, Dictionary<string, string>>> GetMappingConfigsFromBlobFile(string blobConnectionString, string blobContainerName, string blobConfigFileName)
        {

            Dictionary<string, Dictionary<string, string>> dictionary = new Dictionary<string, Dictionary<string, string>>();
            string configStr = string.Empty;
            if (!CloudStorageAccount.TryParse(blobConnectionString, out _storageAccount))
            {
                return dictionary;
            }

            var cloudBlobClient = _storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = cloudBlobClient.GetContainerReference(blobContainerName);
            var blob = _cloudBlobContainer.GetBlobReference(blobConfigFileName);
            if (await blob.ExistsAsync())
            {
                using (var reader = new StreamReader(await blob.OpenReadAsync()))
                {
                    configStr = reader.ReadToEnd();
                }
            }
            var configObj = JObject.Parse(configStr);

            foreach (var property in configObj.Properties())
            {
                var mappingList = JsonConvert.DeserializeObject<List<JObject>>(property.Value.ToString());
                Dictionary<string, string> dict = new Dictionary<string, string>();
                foreach (var mapping in mappingList)
                {
                    foreach (var childProperty in mapping.Properties())
                    {
                        dict.Add(childProperty.Name, childProperty.Value.Value<string>());
                    }
                }
                dictionary.Add(property.Name, dict);
            }

            return dictionary;
        }

        public async Task<string> GetContentFromConfigurationFile(string blobConnectionString, string blobContainerName, string blobConfigFileName)
        {
            var fileContent = string.Empty;
            if (CloudStorageAccount.TryParse(blobConnectionString, out _storageAccount))
            {
                CloudBlobClient cloudBlobClient = _storageAccount.CreateCloudBlobClient();
                _cloudBlobContainer = cloudBlobClient.GetContainerReference(blobContainerName);
                var blob = _cloudBlobContainer.GetBlobReference(blobConfigFileName);
                if (await blob.ExistsAsync())
                {
                    using (StreamReader reader = new StreamReader(await blob.OpenReadAsync()))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
                else
                {
                    throw new StorageException("blobContainerName: " + blobContainerName + ";" + "blobConfigFileName: " + blobConfigFileName);
                }

            }
            return fileContent;
        }
    }
}
