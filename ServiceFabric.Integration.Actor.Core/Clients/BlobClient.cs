using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System.IO;
using System.Threading.Tasks;

namespace Integration.Common.Actor.Clients
{
    public class BlobClient
    {
        public string StorageAccountName;
        public string StorageAccountKey;
        public CloudStorageAccount StorageAccount;

        public BlobClient(string storageAccountKey, string storageAccountName)
        {
            StorageAccountKey = storageAccountKey;
            StorageAccountName = storageAccountName;
            StorageAccount = new CloudStorageAccount(
                new StorageCredentials(
                    StorageAccountName,
                    StorageAccountKey), true);
        }

        public async Task<Stream> ReadBlobAsync(string containerName, string fileName)
        {
            var blobClient = StorageAccount.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            await container.CreateIfNotExistsAsync();
            var blockBlob = container.GetBlockBlobReference(fileName);
            await blockBlob.FetchAttributesAsync();
            return await blockBlob.OpenReadAsync();
        }
    }
}