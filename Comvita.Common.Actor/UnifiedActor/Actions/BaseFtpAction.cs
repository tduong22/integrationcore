using Comvita.Common.Actor.FtpClient;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Utility.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseFtpAction : BaseAction
    {
        protected readonly IActorFtpClient ActorFtpClient;
        protected readonly IConfigKeyVault ConfigKeyVault;
        protected readonly IFtpPolicyRegistry FtpPolicyRegistry;
        protected readonly IFtpStorage FtpStorage;

        protected BaseFtpAction(IActorFtpClient actorFtpClient, IConfigKeyVault configKeyVault,
            IFtpPolicyRegistry ftpPolicyRegistry, IFtpStorage ftpStorage) : base()
        {
            ActorFtpClient = actorFtpClient;
            ConfigKeyVault = configKeyVault;
            FtpPolicyRegistry = ftpPolicyRegistry;
            FtpStorage = ftpStorage;
        }

        public async Task<FtpClientResponse> FtpReadAsync(FtpConfig config, CancellationToken cancellationToken)
        {
            Func<string, Task<string>> myFunc = ConfigKeyVault.GetSecureSecret;
            FtpClientResponse response = new FtpClientResponse { ErrorMessage = "", Status = "", FileData = new List<FtpClientResponseFile>() };
            try
            {
                if (!string.IsNullOrEmpty(config.AzureBlobStorage.StorageAccountName))
                {
                    Func<string, string, string, MemoryStream, string, string, bool> copyFileFunc = FtpStorage.CopyFile;
                    // Retry the following call according to the policy - X times.
                    response = await ActorFtpClient.ReadAsync(config, myFunc, copyFileFunc);
                }
                else
                {
                    Func<string, string, string, MemoryStream, bool> copyFileFunc = FtpStorage.CopyFile;
                    response = await ActorFtpClient.ReadAsync(config, myFunc, copyFileFunc);
                }

            }
            catch (Exception e)
            {
                await ActorFtpClient.Stop();
                Logger.LogError(e, $"[FtpReadAsync] Failed to create FTP Client: " + e.Message);
            }

            return response;
        }

        public async Task<FtpClientResponse> FtpWriteAsync(FtpConfig config, string data, string fileName, CancellationToken cancellationToken)
        {
            Func<string, Task<string>> myFunc = ConfigKeyVault.GetSecureSecret;

            FtpClientResponse response = new FtpClientResponse { ErrorMessage = "", Status = "", FileData = new List<FtpClientResponseFile>() };
            try
            {
                response = await ActorFtpClient.WriteAsync(config, myFunc, Encoding.UTF8.GetBytes(data), fileName);
            }
            catch (Exception e)
            {
                await ActorFtpClient.Stop();
                Logger.LogError(e, $"[FtpWriteAsync] Failed to write file: " + e.Message);
            }

            return response;
        }
    }
}
