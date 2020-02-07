using Integration.Common.Utility.Interfaces;
using Microsoft.Azure.Storage;
using Microsoft.Azure.Storage.Auth;
using Microsoft.Azure.Storage.Blob;
using System;
using System.IO;

namespace Comvita.Common.Actor.FtpClient
{
    public class FtpStorage : IFtpStorage
    {
        private CloudStorageAccount _storageAccount;

        private readonly IFtpPolicyRegistry _ftpPolicyRegistry;

        public FtpStorage(IFtpPolicyRegistry ftpPolicyRegistry)
        {
            _ftpPolicyRegistry = ftpPolicyRegistry;
        }

        public bool CopyFile(string path, string name, string policyName, Stream stream)
        {
            var policy = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.StorageRetryAndWaitPolicy);

            try
            {

                policy.Execute(() =>
                {
                    var blobClient = _storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference(path);
                    container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
                    var blockBlob = container.GetBlockBlobReference(name);

                    using (stream)
                    {
                        blockBlob.UploadFromStreamAsync(stream).GetAwaiter().GetResult();
                    }
                    //TODO: FIX 
                    return true;
                });

                //TODO: FIX 
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool CopyFile(string path, string name, string policyName, MemoryStream stream)
        {
            throw new NotImplementedException("CopyFile(string path, string name, string policyName, MemoryStream stream) not implemented");
        }

        public bool CopyFile(string path, string name, string policyName, MemoryStream stream, string storageAccountName, string storageAccountKey)
        {
            var policy = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.StorageRetryAndWaitPolicy);
            try
            {

                policy.Execute(() =>
                {
                    _storageAccount = new CloudStorageAccount(
                        new StorageCredentials(
                            storageAccountName,
                            storageAccountKey), true);
                    var blobClient = _storageAccount.CreateCloudBlobClient();
                    var container = blobClient.GetContainerReference(path);
                    container.CreateIfNotExistsAsync().GetAwaiter().GetResult();
                    var blockBlob = container.GetBlockBlobReference(name);

                    using (stream)
                    {
                        blockBlob.UploadFromStreamAsync(stream).GetAwaiter().GetResult();

                    }
                    //TODO: FIX 
                    return true;
                });

                //TODO: FIX 
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

    }
}
