using System;
using System.Fabric;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.Interfaces;
using Integration.Common.Actor.Clients;
using Integration.Common.Logging;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;
using Newtonsoft.Json.Linq;

namespace Comvita.Common.Actor.BaseService
{
    public class FtpSchedulerActorService : CommonActorService
    {
        protected BlobClient _blobClient;
        protected IServiceConfiguration ServiceConfiguration;
        protected string SectionKeyName;
        protected string SchedulerActorServiceName;

        public FtpSchedulerActorService(StatefulServiceContext context,
                                     ActorTypeInformation actorTypeInfo,
                                     ILoggerFactory loggerFactory,
                                     IServiceConfiguration serviceConfiguration,
                                     string schedulerActorServiceName,
                                     string sectionKeyName = null,
                                     LoggerConfiguration loggerConfiguration = null,
                                     Func<ActorService, ActorId, ActorBase> actorFactory = null,
                                     Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
                                     IActorStateProvider stateProvider = null,
                                     ActorServiceSettings settings = null) : base(context, actorTypeInfo, loggerFactory, loggerConfiguration, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            ServiceConfiguration = serviceConfiguration;
            SectionKeyName = sectionKeyName ?? this.GetType().Name;
            SchedulerActorServiceName = schedulerActorServiceName;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            await base.RunAsync(cancellationToken);
            await HandleActorBlob(cancellationToken);
        }

        private async Task HandleActorBlob(CancellationToken cancellationToken)
        {
            try
            {
                var _config = ServiceConfiguration.GetConfigSection("BlobStorageForFtp");
                _blobClient = new BlobClient(_config["StorageAccountKey"], _config["StorageAccountName"]);
                var ftpActorListStream = await _blobClient.ReadBlobAsync(_config["ContainerName"], _config["FileName"]);
                using (var reader = new StreamReader(ftpActorListStream))
                {
                    string content = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var actorObject = JObject.Parse(content);
                        var arrayActor = actorObject[SectionKeyName].Values<JObject>();
                        foreach (var ftpObject in arrayActor)
                        {
                            var ftpOption = ftpObject.ToObject<FtpOption>();
                            InitSchedulerActor(ftpOption, cancellationToken);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[HandleActorBlob] {GetType().Name} Failed to handler blob : {ex.Message} ");
                throw;
            }
        }


        private void InitSchedulerActor(FtpOption ftpOption, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var data = MessagePackSerializer.Serialize(ftpOption);
                var proxy = ActorProxy.Create<IBaseMessagingActor>(new ActorId($"scheduler_{ftpOption.Domain}"),
                    new Uri($"{Context.CodePackageActivationContext.ApplicationName}/{SchedulerActorServiceName}"));
                proxy.ChainProcessMessageAsync(new ActorRequestContext(this.GetType().Name), data, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError(e, $"[InitSchedulerActor] Failed to init scheduler actor: " + e.Message, ftpOption);
                throw;
            }
        }



    }
}
