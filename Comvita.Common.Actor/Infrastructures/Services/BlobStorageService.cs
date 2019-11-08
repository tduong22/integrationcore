using Comvita.Common.Actor.Interfaces;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Infrastructures.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly CloudStorageAccount _storageAccount;
        private readonly CloudBlobClient _cloudBlobClient;
        private readonly CloudBlobContainer _cloudBlobContainer;
        private IDictionary<string, string> _settings;

        public BlobStorageService(IDictionary<string, string> configurations)
        {
            _settings = configurations;

            if (!CloudStorageAccount.TryParse(_settings["ConnectionString"], out _storageAccount))
                throw new Exception($"Can not init blobstorage account {_settings["ConnectionString"]}");

            _cloudBlobClient = _storageAccount.CreateCloudBlobClient();
            _cloudBlobContainer = _cloudBlobClient.GetContainerReference(_settings["ContainerName"]);
        }

        public async Task CreateFileAsync(string filename, byte[] fileData)
        {
            var blob = _cloudBlobContainer.GetBlockBlobReference(filename);
            blob.Properties.ContentType = "application/json";
            await blob.UploadFromByteArrayAsync(fileData, 0, fileData.Length);
        }

        public async Task DeleteFileAsync(string filename)
        {
            var blob = _cloudBlobContainer.GetBlockBlobReference(filename);
            await blob.DeleteIfExistsAsync();
        }

        public async Task<string> ReadFileContentAsync(string filename)
        {
            string configs = string.Empty;
            var blob = _cloudBlobContainer.GetBlobReference(filename);
            if (await blob.ExistsAsync())
            {
                using (var reader = new StreamReader(await blob.OpenReadAsync()))
                {
                    configs = reader.ReadToEnd();
                }
            }

            return configs;
        }

        public async Task<T> ReadFileContentAsync<T>(string filename)
        {
            string configs = string.Empty;
            var blob = _cloudBlobContainer.GetBlobReference(filename);
            if (await blob.ExistsAsync())
            {
                using (var reader = new StreamReader(await blob.OpenReadAsync()))
                {
                    configs = reader.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(configs);
        }
    }
}