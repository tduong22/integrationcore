using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.FtpClient
{
    public interface IFtpStorage
    {
        bool CopyFile(string path, string name, string policyName, Stream stream);

        bool CopyFile(string path, string name, string policyName, MemoryStream stream);

        bool CopyFile(string path, string name, string policyName, MemoryStream stream, string storageAccountName, string storageAccountKey);
    }

    public class FtpClientResponse
    {
        public string Status { get; set; }
        public string ErrorMessage { get; set; }
        public List<FtpClientResponseFile> FileData { get; set; }
    }

    public class FtpActorResponse
    {
        public string ActorId { get; set; }
        public string ErrorMessage { get; set; }
        public FtpConfig Config { get; set; }
    }

    public class FtpClientResponseFile
    {
        public string Status { get; set; }
        public string FileName { get; set; }
        public string ErrorMessage { get; set; }
        public byte[] Data { get; set; }
    }

    public interface IActorFtpClient
    {
        Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePasswordFunc, Func<string, string, string, MemoryStream, bool> copyFileFunc);

        Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePasswordFunc, Func<string, string, string, MemoryStream, string, string, bool> copyFileFunc);

        Task<FtpClientResponse> WriteAsync(FtpConfig config, Func<string, Task<string>> securePasswordFunc, byte[] data, string fileName);

        Task<bool> Stop();
    }

    [DataContract]
    public struct FtpConfig
    {
        [DataMember]
        public Ftp Ftp { get; set; }

        [DataMember]
        public string EventName { get; set; }

        [DataMember]
        public AzureBlobStorage AzureBlobStorage { get; set; }

        [DataMember]
        public int Freq { get; set; }

        [DataMember]
        public Retry Retry { get; set; }

        [DataMember]
        public int Parellelism { get; set; }

        [DataMember]
        public string Encoding { get; set; }
    }

    [DataContract]
    public struct Retry
    {
        [DataMember]
        public string RetryPolicy { get; set; }

        [DataMember]
        public string FtpRetryPolicy { get; set; }

        [DataMember]
        public string StorageRetryPolicy { get; set; }
    }

    [DataContract]
    public struct AzureBlobStorage
    {
        [DataMember]
        public string ContainerName { get; set; }

        [DataMember]
        public string Retainfor { get; set; }

        [DataMember]
        public string Overwrite { get; set; }

        [DataMember]
        public string StorageAccountName { get; set; }

        [DataMember]
        public string StorageKeyVault { get; set; }
    }

    [DataContract]
    public struct AzureKeyVault
    {
        [DataMember]
        public string SecretName { get; set; }
    }

    [DataContract]
    public struct Credentials
    {
        [DataMember]
        public string Username { get; set; }

        [DataMember]
        public AzureKeyVault AzureKeyVault { get; set; }
    }

    [DataContract]
    public struct Ftp
    {
        [DataMember]
        public string Host { get; set; }

        [DataMember]
        public int Port { get; set; }

        [DataMember]
        public string Path { get; set; }

        [DataMember]
        public string FilenameRegex { get; set; }

        [DataMember]
        public string ArchivedPath { get; set; }

        [DataMember]
        public string DataConnectionType { get; set; }

        [DataMember]
        public string EncryptionMode { get; set; }

        [DataMember]
        public string Protocol { get; set; }

        [DataMember]
        public string ValidateServerCertificate { get; set; }

        [DataMember]
        public Credentials Credentials { get; set; }
    }
}