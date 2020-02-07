using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FluentFTP;

namespace Comvita.Common.Actor.FtpClient
{
    public class ActorFtpClient : IActorFtpClient
    {
        #region Private Members

        private readonly IFtpPolicyRegistry _ftpPolicyRegistry;
        private FluentFTP.FtpClient _ftpClient;

        private ILogger _logger;

        #endregion Private Members

        #region Constructors

        public ActorFtpClient(IFtpPolicyRegistry ftpPolicyRegistry, ILoggerFactory loggerFactory)
        {
            _ftpPolicyRegistry = ftpPolicyRegistry;
            _logger = loggerFactory.CreateLogger<ActorFtpClient>();
        }

        #endregion Constructors

        #region FTP Methods

        private async Task CreateFtpClientAsync(FtpConfig config, Func<string, Task<string>> securePasswordCallBack)
        {
            var passwd = await securePasswordCallBack.Invoke(config.Ftp.Credentials.AzureKeyVault.SecretName);
            _ftpClient = (config.Ftp.Protocol.Equals(FtpConstants.Ftps, StringComparison.InvariantCultureIgnoreCase) || config.Ftp.Port.Equals(990)) ?
                     new FluentFTP.FtpClient(config.Ftp.Host)
                     {   //FTPS explicitly or Port = 990 => automatically create FTPS
                         Credentials = new NetworkCredential(config.Ftp.Credentials.Username, passwd),
                         Port = config.Ftp.Port,
                         EnableThreadSafeDataConnections = true,
                         DataConnectionType = config.Ftp.DataConnectionType == FtpConstants.FtpActiveMode ? FtpDataConnectionType.AutoActive : FtpDataConnectionType.AutoPassive,
                         EncryptionMode = config.Ftp.Port.Equals(990) ? FtpEncryptionMode.Implicit : FtpEncryptionMode.Explicit,
                         DataConnectionReadTimeout = 30000
                     }
                     :
                     new FluentFTP.FtpClient(config.Ftp.Host)
                     {
                         Credentials = new NetworkCredential(config.Ftp.Credentials.Username, await securePasswordCallBack.Invoke(config.Ftp.Credentials.AzureKeyVault.SecretName)),
                         Port = config.Ftp.Port,
                         EnableThreadSafeDataConnections = true,
                         Encoding = Encoding.UTF8,
                         DataConnectionType = config.Ftp.DataConnectionType == FtpConstants.FtpActiveMode ? FtpDataConnectionType.AutoActive : FtpDataConnectionType.AutoPassive,
                         DataConnectionReadTimeout = 30000
                     };

            _ftpClient.ValidateCertificate += (control, e) =>
            {
                e.Accept = (string.IsNullOrEmpty(config.Ftp.ValidateServerCertificate)
                            || config.Ftp.ValidateServerCertificate.Equals("false", StringComparison.InvariantCultureIgnoreCase)) ? true
                    : (e.Certificate.GetRawCertDataString() == config.Ftp.ValidateServerCertificate || e.PolicyErrors == SslPolicyErrors.None);
            };

            _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpConnectPolicy).Execute(() => _ftpClient.Connect());
        }

        public async Task<FtpClientResponse> ReadAsync(FtpConfig config, Func<string, Task<string>> securePasswordCallBack,
            Func<string, string, string, MemoryStream, string, string, bool> copyFileFunc)
        {
            var response = new FtpClientResponse { ErrorMessage = FtpConstants.NoError, Status = FtpConstants.SuccessStatus, FileData = new List<FtpClientResponseFile>() };

            try
            {
                await CreateFtpClientAsync(config, securePasswordCallBack);

                Func<FtpListItem, bool> predicate = f => f.Type == FtpFileSystemObjectType.File;
                if (!string.IsNullOrEmpty(config.Ftp.FilenameRegex))
                {
                    var rx = new Regex(config.Ftp.FilenameRegex, RegexOptions.IgnoreCase);
                    predicate = f => f.Type == FtpFileSystemObjectType.File && rx.IsMatch(f.Name);
                }

                var listOfFiles = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy)
                    .Execute((context) => _ftpClient.GetListing(config.Ftp.Path).Where(predicate), new Polly.Context($"LIST_FILE_{config.Ftp.Path}"));

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

                foreach (var item in listOfFiles)
                {
                    bool copied = false;
                    bool isArchived = false;
                    try
                        {
                            var fileName = item.Name;
                            using (var istream = await _ftpClient.OpenReadAsync(item.FullName))
                            {
                                MemoryStream memoryStream = new MemoryStream();
                                istream.CopyTo(memoryStream);
                                memoryStream.Position = 0;
                                copied = copyFileFunc.Invoke(config.AzureBlobStorage.ContainerName,
                                    fileName,
                                    config.Retry.StorageRetryPolicy, memoryStream, config.AzureBlobStorage.StorageAccountName, resolvedStorageAccountKey);
                            }
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
                            response.FileData.Add(fres);
                    }
                        catch (Exception ex)
                        {
                           _logger.LogError(ex, $"ActorFtpClient failed to process the file {item.Name}, copy: {copied}, archived {isArchived}. Error: {ex.Message}");
                        }
                    }
                await Stop();
            }
            catch (Exception e)
            {
                response.ErrorMessage = response.ErrorMessage + e.Message;
                response.Status = FtpConstants.FailureStatus;
                 _logger.LogError(e, $"ActorFtpClient failed to start creating the client of {config.Ftp.Host} : {e.Message}");
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

                Func<FtpListItem, bool> predicate = f => f.Type == FtpFileSystemObjectType.File;
                if (!string.IsNullOrEmpty(config.Ftp.FilenameRegex))
                {
                    var rx = new Regex(config.Ftp.FilenameRegex, RegexOptions.IgnoreCase);
                    predicate = f => f.Type == FtpFileSystemObjectType.File && rx.IsMatch(f.Name);
                }

                var listOfFiles = _ftpPolicyRegistry.GetPolicy(FtpPolicyName.FtpCommandPolicy)
                    .Execute((context) => _ftpClient.GetListing(config.Ftp.Path).Where(predicate), new Polly.Context($"LIST_FILE_{config.Ftp.Path}"));

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

        #endregion FTP Methods
    }
}