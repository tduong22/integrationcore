using System;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Comvita.Common.EventBus.BaseService;
using Comvita.Common.EventBus.EventBusOption;
using Integration.Common.Logging;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using MessagePack;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseService
{
    /// <summary>
    /// Actor Service tied to service bus listeners
    /// Create listeners by passing ServiceBusOptions retrieved from blob.
    /// Required Service Fabric Config:
    /// SectionName: (your service class name). List of keys: ConnectionString, ContainerName, FileName
    /// If config is service fabric mode, just need to provide connection string with section name = (your service class name)
    /// </summary>
    public class ServiceBusListenerActorService : CommonActorService
    {
        protected readonly IBlobStorageConfiguration BlobStorageConfiguration;
        protected readonly IServiceConfiguration ServiceConfig;
        protected readonly Func<ServiceBusOption, string> ActorIdRetriever;
        protected readonly Func<string> ListenerActorServiceUri;
        protected readonly string SectionKeyName;

        public const string CONNECTION_STRING_KEY = "ConnectionString";
        public const string CONTAINER_NAME_KEY = "ContainerName";
        public const string FILE_NAME_KEY = "FileName";
        public readonly string ActionName;

        public ConfigMode ConfigMode;

        public ServiceBusListenerActorService(StatefulServiceContext context, ActorTypeInformation actorTypeInfo,
            IBlobStorageConfiguration blobStorageConfiguration,
            IServiceConfiguration serviceConfig,
            ILoggerFactory loggerFactory,
            Func<string> listenerActorServiceUri,
            Func<ServiceBusOption, string> actorIdRetriever,
            ConfigMode configMode = ConfigMode.BlobConfig,
            string actionName = null,
            LoggerConfiguration loggerConfiguration = null,
            string sectionName = null,
            Func<ActorService, ActorId, ActorBase> actorFactory = null,
            Func<ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null,
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null)
            : base(context, actorTypeInfo, loggerFactory, loggerConfiguration, actorFactory, stateManagerFactory, stateProvider, settings)
        {
            ServiceConfig = serviceConfig;
            BlobStorageConfiguration = blobStorageConfiguration;
            ActorIdRetriever = actorIdRetriever;
            ListenerActorServiceUri = listenerActorServiceUri;
            SectionKeyName = sectionName ?? GetType().Name;
            Logger = loggerFactory.CreateLogger<ServiceBusListenerActorService>();
            ConfigMode = configMode;
            ActionName = actionName;
        }

        protected override async Task RunAsync(CancellationToken cancellationToken)
        {
            // need to call base run async to prepare for the state or else actor state will throw errors ReminderLoadInProgressException
            await base.RunAsync(cancellationToken);
            var config = ServiceConfig.GetConfigSection(SectionKeyName);
            //read the blob and init scheduler actor

            if (ConfigMode == ConfigMode.BlobConfig)
            {
                var configList = await BlobStorageConfiguration.GetConfigsFromBlobFile<ServiceBusOption>(
                    config[CONNECTION_STRING_KEY],
                    config[CONTAINER_NAME_KEY],
                    config[FILE_NAME_KEY],
                    $"{SectionKeyName}");
                foreach (var option in configList)
                {
                    Logger.LogInformation($"[{SectionKeyName}] Initialize Listener Actor", option);
                    await InitScalingListenersAsync(option, cancellationToken);
                }
            }
            else if (ConfigMode == ConfigMode.ServiceFabricConfig)
            {
                var serviceBusOption = new ServiceBusOption()
                {
                    ConnectionString = config[CONNECTION_STRING_KEY],
                    ClientMode = ClientMode.Receiving,
                    SubscriptionRequireSession = false,
                    SubscriptionName = $"{SectionKeyName}_sub_no_sessionId"
                };
                await InitScalingListenersAsync(serviceBusOption, cancellationToken);
            }
        }

        protected Task InitScalingListenersAsync(ServiceBusOption sbConfig, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var data = MessagePackSerializer.Serialize(sbConfig);

                var proxy = ActorProxy.Create<IBaseMessagingActor>(new ActorId(ActorIdRetriever.Invoke(sbConfig)),
                    new Uri(
                        $"{Context.CodePackageActivationContext.ApplicationName}/{ListenerActorServiceUri.Invoke()}"));

                var actionName = ActionName ?? nameof(IDefaultServiceBusListenerAction);

                proxy.ChainProcessMessageAsync(new ActorRequestContext(SectionKeyName, actionName, Guid.NewGuid().ToString()), data, cancellationToken).GetAwaiter().GetResult();

            }
            catch (Exception e)
            {
                //failed to call scheduler actor
                Logger.LogError(e, $"[{SectionKeyName}] Failed to init the listener actor: " + e.Message, sbConfig);
            }
            return Task.CompletedTask;
        }
    }
}
