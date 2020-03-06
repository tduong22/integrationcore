using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.Models;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultFtpDispatchAction : BaseFtpAction, IDefaultFtpDispatchAction
    {
        public DefaultFtpDispatchAction(IActorFtpClient actorFtpClient, IConfigKeyVault configKeyVault, IFtpPolicyRegistry ftpPolicyRegistry, IFtpStorage ftpStorage)
              : base(actorFtpClient, configKeyVault, ftpPolicyRegistry, ftpStorage)
        {
        }

        /*
        public override async Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload, CancellationToken cancellationToken)
        {
            var dispatchRequest = Deserialize<DispatchRequest>(payload);
            await ChainRequestAsync(actorRequestContext, payload, typeof(DispatchRequest), dispatchRequest.DispatchActionName, cancellationToken);
        }*/

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public async Task InvokeDispatchReadRequest(DispatchRequest requestRead)
        {
            FtpClientResponse response = new FtpClientResponse { ErrorMessage = "", Status = "", FileData = new List<FtpClientResponseFile>() };
            var cancellationToken = CancellationToken.None;

            var ftpOption = (FtpOption)requestRead.DeserializeRequestData();
            response = await FtpReadAsync(ftpOption.FtpConfig, cancellationToken);
            foreach (var file in response.FileData)
            {
                BlobStorageFileInfo fileInfo = new BlobStorageFileInfo()
                {
                    Container = ftpOption.FtpConfig.AzureBlobStorage.ContainerName,
                    FileName = file.FileName,
                    SourceFilePath = ftpOption.FtpConfig.Ftp.Host + '/' + ftpOption.FtpConfig.Ftp.Path
                };

                if (requestRead.ResponseActorInfo != null)
                {
                    var returnActorId =
                        requestRead.ResponseActorInfo?.ActorId ??
                        Guid.NewGuid().ToString();
                    var returnActorServiceUri =
                        requestRead.ResponseActorInfo?.ActorServiceUri;
                    var returnActionName = requestRead.ResponseActorInfo?.ActionName;
                    var newExecutableOrder = new ExecutableOrchestrationOrder()
                    {
                        ActorId = returnActorId,
                        ActorServiceUri = returnActorServiceUri
                    };

                    await ChainNextActorsAsync<IFtpResponseParserAction>(c=>c.InvokeHandleFtpReadResponse(fileInfo), new ActorRequestContext(Id.ToString(), returnActionName,
                    Guid.NewGuid().ToString(), CurrentFlowInstanceId), newExecutableOrder, cancellationToken);
                }
            }
        }

        public async Task InvokeDispatchWriteRequest(DispatchRequest requestWrite)
        {
            FtpClientResponse response = new FtpClientResponse { ErrorMessage = "", Status = "", FileData = new List<FtpClientResponseFile>() };
            var cancellationToken = CancellationToken.None;

            var ftpWriterOption = Deserialize<FtpWriterOption>(requestWrite.RequestDataBinary);
            response = await FtpWriteAsync(ftpWriterOption.FtpConfig, ftpWriterOption.WriteData, ftpWriterOption.FileName, cancellationToken);
            Logger.LogInformation($"{CurrentActor} FtpWriteAsync finished with response code {response.Status}. Data file: {ftpWriterOption.WriteData}");

            if (requestWrite.ResponseActorInfo != null)
            {
                var returnActorId =
                    requestWrite.ResponseActorInfo?.ActorId ??
                    Guid.NewGuid().ToString();
                var returnActorServiceUri =
                    requestWrite.ResponseActorInfo?.ActorServiceUri;
                var returnActionName = requestWrite.ResponseActorInfo?.ActionName;
                var newExecutableOrder = new ExecutableOrchestrationOrder()
                {
                    ActorId = returnActorId,
                    ActorServiceUri = returnActorServiceUri
                };

                await ChainNextActorsAsync<IFtpResponseParserAction>(c => c.InvokeHandleFtpWriteResponse(), new ActorRequestContext(Id.ToString(), returnActionName,
                Guid.NewGuid().ToString(), CurrentFlowInstanceId), newExecutableOrder, cancellationToken);
            }
        }
    }
}
