using FluentFTP;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.FtpClient
{
    public class SingletonActorFtpClient : IActorFtpClient
    {
        #region Private Members

        private readonly IFtpPolicyRegistry _ftpPolicyRegistry;
        private FluentFTP.FtpClient _ftpClient;

        private IDictionary<string, FluentFTP.FtpClient> clientCollection = new Dictionary<string, FluentFTP.FtpClient>();

        #endregion Private Members

        public SingletonActorFtpClient(IFtpPolicyRegistry ftpPolicyRegistry)
        {
            _ftpPolicyRegistry = ftpPolicyRegistry;
        }

        private async Task CreateFtpClientAsync(FtpConfig config, Func<string, Task<string>> securePasswordCallBack)
        {
            try
            {
                var passwd = await securePasswordCallBack.Invoke(config.Ftp.Credentials.AzureKeyVault.SecretName);
                var isRegistered = clientCollection.TryGetValue(config.Ftp.Host, out _ftpClient);
                if (!isRegistered)
                {
                    var ftpClient = (config.Ftp.Protocol.Equals(FtpConstants.Ftps, StringComparison.InvariantCultureIgnoreCase) || config.Ftp.Port.Equals(990)) ?
                             new FluentFTP.FtpClient(config.Ftp.Host)
                             {   //FTPS explicitly or Port = 990 => automatically create FTPS
                             Credentials = new NetworkCredential(config.Ftp.Credentials.Username, passwd),
                                 Port = config.Ftp.Port,
                                 EnableThreadSafeDataConnections = true,
                                 DataConnectionType = config.Ftp.DataConnectionType == FtpConstants.FtpActiveMode ? FtpDataConnectionType.AutoActive : FtpDataConnectionType.AutoPassive,
                                 EncryptionMode = config.Ftp.Port.Equals(990) ? FtpEncryptionMode.Implicit : FtpEncryptionMode.Explicit
                             }
                             :
                             new FluentFTP.FtpClient(config.Ftp.Host)
                             {
                                 Credentials = new NetworkCredential(config.Ftp.Credentials.Username, await securePasswordCallBack.Invoke(config.Ftp.Credentials.AzureKeyVault.SecretName)),
                                 Port = config.Ftp.Port,
                                 EnableThreadSafeDataConnections = true
                             };

                    ftpClient.ValidateCertificate += (control, e) =>
                    {
                        e.Accept = (string.IsNullOrEmpty(config.Ftp.ValidateServerCertificate)
                                    || config.Ftp.ValidateServerCertificate.Equals("false", StringComparison.InvariantCultureIgnoreCase)) ? true
                            : (e.Certificate.GetRawCertDataString() == config.Ftp.ValidateServerCertificate || e.PolicyErrors == SslPolicyErrors.None);
                    };
                    _ftpClient = ftpClient;
                    clientCollection.Add(config.Ftp.Host, ftpClient);
                }

                if (_ftpClient != null && !_ftpClient.IsConnected)
                    _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpConnectPolicy).Execute(() => _ftpClient.Connect());
            }
            catch (Exception)
            {
                throw;
            }
        }

        public async Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePasswordCallBack,
            Func<string, string, string, MemoryStream, string, string, bool> copyFileFunc)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };

            try
            {
                if (_ftpClient == null)
                {
                    await CreateFtpClientAsync(config, securePasswordCallBack);
                }
                else
                {
                    if (!_ftpClient.IsConnected)
                        await CreateFtpClientAsync(config, securePasswordCallBack);
                }

                var listOfFiles = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.GetListing(config.Ftp.Path).Where(f => f.Type == FtpFileSystemObjectType.File), new Polly.Context($"LIST_FILE_{config.Ftp.Path}"));

                //config.Ftp.ArchivedPath != config.Ftp.Path is delete mode
                if (!string.IsNullOrEmpty(config.Ftp.ArchivedPath) && config.Ftp.ArchivedPath != config.Ftp.Path)
                {
                    var folderExisted = _ftpClient.DirectoryExists(config.Ftp.ArchivedPath);
                    if (!folderExisted)
                    {
                        _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.CreateDirectory(config.Ftp.ArchivedPath), new Polly.Context("CREATE_ARCHIVE_FOLDER"));
                    }
                }

                var resolvedStorageAccountKey =
                    await securePasswordCallBack.Invoke(config.AzureBlobStorage.StorageKeyVault);

                Parallel.ForEach(
                    listOfFiles,
                    new ParallelOptions { MaxDegreeOfParallelism = config.Parellelism },
                    item =>
                    {
                        try
                        {
                            var fileName = item.Name;
                            using (var istream = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.OpenRead(item.FullName), new Polly.Context($"READ_FILE_{item.FullName}")))
                            {
                                bool isArchived = false;
                                MemoryStream memoryStream = new MemoryStream();
                                istream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                //_telemetryLogger.TrackTrace<FtpActor>("", "[ActorFtpClient] Reading File " + item.FullName, SeverityLevel.Information);
                                var copied = copyFileFunc.Invoke(config.AzureBlobStorage.ContainerName,
                                    fileName,
                                    config.Retry.StorageRetryPolicy, memoryStream, config.AzureBlobStorage.StorageAccountName, resolvedStorageAccountKey);

                                if (copied && !string.IsNullOrEmpty(config.Ftp.ArchivedPath) && config.Ftp.ArchivedPath != config.Ftp.Path)
                                {
                                    isArchived = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.MoveFile(item.FullName, config.Ftp.ArchivedPath + "\\" + item.Name, FtpExists.Overwrite), new Polly.Context("COPY_FILE_TO_ARCHIVE"));
                                }
                                else if (config.Ftp.ArchivedPath == config.Ftp.Path)
                                {
                                    //delete mode
                                    isArchived = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) =>
                                    {
                                        _ftpClient.DeleteFile(item.FullName);
                                        return true;
                                    }, new Polly.Context("DELETE_FILE_TO_ARCHIVE"));
                                }

                                var fres = new FtpClientResponseFile
                                {
                                    Status = (copied && isArchived) ? FtpConstants.SuccessStatus : FtpConstants.FailureStatus,
                                    FileName = item.Name,
                                    ErrorMessage = (copied ? string.Empty : FtpConstants.BlobUploadError) + " " + (isArchived ? string.Empty : FtpConstants.ArchivedError)
                                };

                                var properties = new Dictionary<string, string> { { "payload", JsonConvert.SerializeObject(fres) } };
                                //_telemetryLogger.TrackEvent("", EventConstant.FtpCustomEventName, properties);
                                response.FileData.Add(fres);
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                );

                //_telemetryLogger.TrackMetric("", EventConstant.FtpCustomEventName, 1);
                await Stop();
            }
            catch (FtpNotConnectException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (FtpSecurityNotAvailableException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (Exception e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            return response;
        }

        [Obsolete("This method is not maintained at the moment. Please use the other ReadAsync method")]
        public async Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePasswordCallBack, Func<string, string, string, MemoryStream, bool> copyFileFunc)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };

            try
            {
                await CreateFtpClientAsync(config, securePasswordCallBack);

                var listOfFiles = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.GetListing(config.Ftp.Path).Where(f => f.Type == FtpFileSystemObjectType.File), new Polly.Context($"LIST_FILE_{config.Ftp.Path}"));

                if (!string.IsNullOrEmpty(config.Ftp.ArchivedPath))
                {
                    var folderExisted = _ftpClient.DirectoryExists(config.Ftp.ArchivedPath);
                    if (!folderExisted)
                    {
                        _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.CreateDirectory(config.Ftp.ArchivedPath), new Polly.Context("CREATE_ARCHIVE_FOLDER"));
                    }
                }

                Parallel.ForEach(
                    listOfFiles,
                    new ParallelOptions { MaxDegreeOfParallelism = config.Parellelism },
                    item =>
                    {
                        try
                        {
                            var fileName = item.FullName.Remove(0, 1);
                            using (var istream = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.OpenRead(fileName), new Polly.Context($"READ_FILE_{item.FullName}")))
                            {
                                bool isArchived = false;
                                MemoryStream memoryStream = new MemoryStream();
                                istream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                //_telemetryLogger.TrackTrace<FtpActor>("", "[ActorFtpClient] Reading File " + item.FullName, SeverityLevel.Information);
                                var copied = copyFileFunc.Invoke(config.AzureBlobStorage.ContainerName,
                                        fileName,
                                        config.Retry.StorageRetryPolicy, memoryStream);

                                if (copied && !string.IsNullOrEmpty(config.Ftp.ArchivedPath))
                                {
                                    isArchived = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy).Execute((context) => _ftpClient.MoveFile(item.FullName, config.Ftp.ArchivedPath + "\\" + item.Name, FtpExists.Overwrite), new Polly.Context("COPY_FILE_TO_ARCHIVE"));
                                }

                                var fres = new FtpClientResponseFile
                                {
                                    Status = (copied && isArchived) ? FtpConstants.SuccessStatus : FtpConstants.FailureStatus,
                                    FileName = item.Name,
                                    ErrorMessage = (copied ? string.Empty : FtpConstants.BlobUploadError) + " " + (isArchived ? string.Empty : FtpConstants.ArchivedError)
                                };
                                response.FileData.Add(fres);
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                );
                await Stop();
            }
            catch (FtpNotConnectException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (FtpSecurityNotAvailableException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (Exception e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            return response;
        }

        public async Task<FtpClientResponse> WriteAsync(FtpConfig config, Func<string, Task<string>> securePasswordFunc, byte[] data, string fileName)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };

            try
            {
                await CreateFtpClientAsync(config, securePasswordFunc);
                _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy)
                    .Execute((context) => _ftpClient.Upload(data, $"{config.Ftp.Path}/{fileName}"),
                    new Polly.Context("WRITE_FILE"));
            }
            catch (FtpNotConnectException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (FtpSecurityNotAvailableException e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
            }
            catch (Exception)
            {
                throw;
            }
            return response;
        }

        public async Task<bool> Stop()
        {
            try
            {
                await _ftpClient.DisconnectAsync();
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }
    }
}