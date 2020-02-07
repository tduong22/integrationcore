using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Comvita.Common.Actor.Constants;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Renci.SshNet;

namespace Comvita.Common.Actor.FtpClient
{
    public class ActorSftpClient : IActorFtpClient
    {
        #region Private Members

        private readonly IFtpPolicyRegistry _ftpPolicyRegistry;
        private SftpClient _sftpClient;
        public readonly string SFTPConnection = "SFTPConnection";
        private ILogger _logger;
        #endregion

        #region Constructors

        public ActorSftpClient(IFtpPolicyRegistry ftpPolicyRegistry, ILoggerFactory loggerFactory)
        {
            _ftpPolicyRegistry = ftpPolicyRegistry;
            _logger = loggerFactory.CreateLogger<ActorSftpClient>();
        }

        #endregion

        #region SFTP Methods


        #endregion

        public Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePassword, Func<string, string, string, MemoryStream, bool> copyFileFunc)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Stop()
        {
            try
            {
                _sftpClient.Disconnect();
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                var eventString = JsonConvert.SerializeObject(new { eventName = e.Message, sourceSystem = "TestSystem" });
                return Task.FromResult(false);
            }

        }

        public async Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> myFunc, Func<string, string, string, MemoryStream, string, string, bool> copyFileFunc)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };
            try
            {
                var connectionInfo = await SetUpSftpAsync(config, myFunc);
                _sftpClient = new SftpClient(connectionInfo);
                using (_sftpClient)
                {
                    _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpConnectPolicy).Execute(() => _sftpClient.Connect());

                    var listOfFiles = _sftpClient.ListDirectory(config.Ftp.Path).Where(f => !f.IsDirectory)
                        .Select(f => f)
                        .ToList();
                    if (!string.IsNullOrEmpty(config.Ftp.ArchivedPath))
                    {
                        var folderExisted = _sftpClient.Exists(config.Ftp.ArchivedPath);
                        if (!folderExisted)
                        {
                            _sftpClient.CreateDirectory(config.Ftp.ArchivedPath);
                        }
                    }
                    var resolvedStorageAccountKey =
                    await myFunc.Invoke(config.AzureBlobStorage.StorageKeyVault);


                    foreach (var item in listOfFiles)
                    {
                        bool copied = false;
                        bool isArchived = false;

                        try
                        {
                            var fileName = item.Name;
                            using (var istream = _sftpClient.OpenRead(item.FullName))
                            {
                                MemoryStream memoryStream = new MemoryStream();
                                istream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                copied = copyFileFunc.Invoke(config.AzureBlobStorage.ContainerName,
                                    fileName,
                                    config.Retry.StorageRetryPolicy, memoryStream, config.AzureBlobStorage.StorageAccountName, resolvedStorageAccountKey);

                                if (copied && !string.IsNullOrEmpty(config.Ftp.ArchivedPath))
                                {

                                    var fileNameSplit = item.Name.Split('.');
                                    var newFileName =
                                        fileNameSplit[0] + "_" + DateTime.UtcNow.ToString("MMddyyyy_HHmmss") + "." +
                                        fileNameSplit[1];
                                    var path = config.Ftp.ArchivedPath + "/" + newFileName;

                                    item.MoveTo(path);
                                    isArchived = true;
                                }

                                var fres = new FtpClientResponseFile
                                {
                                    Status = (copied && isArchived) ? FtpConstants.SuccessStatus : FtpConstants.FailureStatus,
                                    FileName = fileName,
                                    ErrorMessage = (copied ? string.Empty : FtpConstants.BlobUploadError) + " " + (isArchived ? string.Empty : FtpConstants.ArchivedError)
                                };
                                response.FileData.Add(fres);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, $"{nameof(ActorSftpClient)} failed to process the file {item.Name}, copy: {copied}, archived {isArchived}. Error: {ex.Message}");
                        }
                    }
                }
                await Stop();
            }

            catch (Exception e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
                _logger.LogError(e, $"{nameof(ActorSftpClient)} failed to start creating the client of {config.Ftp.Host} : {e.Message}");
            }
            return response;
        }

        private async Task<ConnectionInfo> SetUpSftpAsync(FtpConfig config, Func<string, Task<string>> securePassword)
        {
            try
            {
                var passAuthenMethod = new PasswordAuthenticationMethod(config.Ftp.Credentials.Username, await securePassword.Invoke(config.Ftp.Credentials.AzureKeyVault.SecretName));
                var privateKeyAuthenMethod = new PrivateKeyAuthenticationMethod(config.Ftp.Credentials.Username);
                var connectionInfo = new ConnectionInfo(config.Ftp.Host, config.Ftp.Port, config.Ftp.Credentials.Username, passAuthenMethod, privateKeyAuthenMethod);
                return connectionInfo;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<FtpClientResponse> WriteAsync(FtpConfig config, Func<string, Task<string>> securePasswordFunc, byte[] data, string fileName)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };
            var connectionInfo = await SetUpSftpAsync(config, securePasswordFunc);
            if (config.Encoding == null)
            {
                connectionInfo.Encoding = Encoding.UTF8;
            }
            else
            {
                if (config.Encoding == CommonConstants.ENCODING_DEFAULT_VALUE_TEXT)
                {
                    connectionInfo.Encoding = Encoding.Default;
                }
                else
                {
                    connectionInfo.Encoding = Encoding.GetEncoding(config.Encoding);
                }
                
            }
            
            _sftpClient = new SftpClient(connectionInfo);
            using (_sftpClient)
            {
                _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpConnectPolicy).Execute(() => _sftpClient.Connect());
                
                var stream = new MemoryStream();
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute(() =>
                    {
                        _sftpClient.UploadFile(stream, $"{config.Ftp.Path}/{fileName}");
                    });

            }

            return response;
        }
    }
}
