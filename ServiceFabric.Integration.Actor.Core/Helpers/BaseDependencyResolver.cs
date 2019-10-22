using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Autofac;
using Autofac.Integration.ServiceFabric;
using Integration.Common.Actor.UnifiedActor;
using Integration.Common.Actor.Clients;
using Integration.Common.Actor.Interface;
using Integration.Common.Actor.Persistences;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;
using Integration.Common.Utility;
using Integration.Common.Actor.Utilities;
using Integration.Common.Flow;
using Integration.Common.Model;
using Integration.Common.Interface;
using Integration.Common.Utility.Interfaces;
using AutoMapper;
using Integration.Common.Logging;
using MessagePack;

namespace Integration.Common.Actor.Helpers
{
    /// <summary>
    /// Reusable Generics & Common Resolve by Autofac
    /// </summary>
    public partial class BaseDependencyResolver
    {
        public static IContainer Container;
        public static ContainerBuilder Builder;
        public static IServiceConfiguration ServiceConfiguration;
        public static ILoggerFactory LoggerFactory;
        public static ILogger Logger;

        static BaseDependencyResolver()
        {
            Builder = new ContainerBuilder();
            Builder.RegisterServiceFabricSupport();
            RegisterSerializer();
            RegisterConfiguration();
            RegisterLogging(ServiceConfiguration);
            RegisterActorRequestPersistence();
            RegisterActorClient();
        }

        #region Register

        public static void RegisterActorRequestPersistence()
        {
            Builder.RegisterType<ActorRequestPersistence>().As<IActorRequestPersistence>();
        }

        protected static void RegisterBlobConfiguration()
        {
            Builder.RegisterType<BlobStorageConfiguration>().As<IBlobStorageConfiguration>().SingleInstance();
        }

        protected static void RegisterBlobClient(string sectionName = "BlobStorageForQadListener")
        {
            var config = ServiceConfiguration.GetConfigSection(sectionName);
            var blobClient = new BlobClient(config["StorageAccountKey"], config["StorageAccountName"]);
            Builder.RegisterInstance(blobClient).As<BlobClient>();
        }

        public static void RegisterDataPackageReader()
        {
            //register Datapackage reader
            Builder.RegisterType<DataPackageReader>().As<IDataPackageReader>().SingleInstance();
        }

        public static void RegisterConfiguration()
        {
            ServiceConfiguration = new ServiceConfiguration();
            Builder.RegisterType<ServiceConfiguration>().As<IServiceConfiguration>().SingleInstance();
        }

        public static void RegisterLogging(IServiceConfiguration serviceConfig, LoggerConfiguration loggerConfig = null,
            string logSection = "Logging", string systemKey = "SystemKey", string businessKey = "BusinessKey",
            string levelKey = "Level")
        {
            if (loggerConfig == null)
            {
                loggerConfig = new LoggerConfiguration();
                var logConfigSection = serviceConfig.GetConfigSection(logSection);
                loggerConfig.SystemInstrumentationKey = logConfigSection[systemKey];
                loggerConfig.BusinessInstrumentationKey = logConfigSection[businessKey];
                loggerConfig.Level = logConfigSection[levelKey];
            }

            LoggerFilterOptions filterOptions = new LoggerFilterOptions();
            filterOptions.AddFilter("", loggerConfig.LogLevel);
            var config = TelemetryConfiguration.CreateDefault();

            config.InstrumentationKey = loggerConfig.BusinessInstrumentationKey;
            IOptions<TelemetryConfiguration> telemeryOptions = Options.Create(config);
            IOptions<ApplicationInsightsLoggerOptions> configureApplicationInsightsLoggerOptions = Options.Create(
                new ApplicationInsightsLoggerOptions());

            ILoggerFactory loggerFactory =
                new LoggerFactory(
                    new[]
                    {
                        new ApplicationInsightsLoggerProvider(telemeryOptions,
                            configureApplicationInsightsLoggerOptions)
                    }, filterOptions);
            LoggerFactory = loggerFactory;
            Logger = loggerFactory.CreateLogger<BaseDependencyResolver>();
            Builder.RegisterInstance(loggerConfig);
            Builder.RegisterInstance(loggerFactory)
                .As<ILoggerFactory>();
        }

        public static void RegisterAutoMapperMapping(IEnumerable<Profile> listOfMappingProfile)
        {
            Builder.Register(c => new MapperConfiguration(cfg =>
            {
                foreach (var profile in listOfMappingProfile)
                {
                    cfg.AddProfile(profile);
                }

            })).SingleInstance();
            Builder.Register(c => c.Resolve<MapperConfiguration>().CreateMapper(c.Resolve)).As<IMapper>().SingleInstance();
        }

        public static void RegisterStorage(Uri storageActorUri)
        {
            var storageServiceInfo = new StorageServiceInfo()
            {
                ServiceUri = storageActorUri,
            };
            Builder.RegisterInstance(storageServiceInfo).As<StorageServiceInfo>();
            Builder.RegisterType<MessagingActorStorage>().As<IKeyValueStorage<string>>();
        }

        public static void RegisterFlowService(Uri flowServiceUri, string key = "default")
        {
            var flowServiceInfo = new FlowServiceInfo()
            {
                ServiceUri = flowServiceUri,
            };
            Builder.RegisterInstance(flowServiceInfo).As<FlowServiceInfo>();
            Builder.RegisterType<ActorServiceOrchestrationFlow>().Keyed<IAsyncOrchestrationFlow<Step>>(key);
        }

        public static void RegisterSerializer()
        {
            SetUpSerializer();
            Builder.RegisterType<MessagePackBinaryMessageSerializer>().As<IBinaryMessageSerializer>().SingleInstance();
        }

        public static void RegisterActorClient()
        {
            Builder.RegisterType<MessagingActorClient>().As<IActorClient>();
        }

        /// <summary>
        /// Scan all IAction in current calling assembly and register through IoC container
        /// </summary>
        public static void RegisterIAction()
        {
            //get assemblies to register with attribute action name
            var assembly = Assembly.GetCallingAssembly();
            Builder.RegisterAssemblyTypes(assembly)
                .AssignableTo<IAction>()
                .AssignableTo<IRemotableAction>()
                .Where(t => !t.IsInterface && !t.IsAbstract)
                .Keyed<IAction>(t => CalculateIActionKey(t)).InstancePerLifetimeScope();

        }

        private static string CalculateIActionKey(Type actionType)
        {
            //get the first interface implemented the IRemotableAction but not the IRemotableAction itself
            var interfaces = actionType.GetInterfaces().Where(i => i.Name != nameof(IRemotableAction) && i.GetInterfaces().Select(c => c.Name).Contains(nameof(IRemotableAction)));
            if (interfaces != null && interfaces.Count() > 1) throw new InvalidOperationException($"Type {actionType.FullName } has implemented more than 1 remoteable interface which cant be registered. Please make sure only one interface is found on this type.");
            var correctInterface = interfaces.FirstOrDefault();

            //if not found
            if (correctInterface == null) return string.Empty;
            //if a generic type
            if (correctInterface.IsGenericType) throw new NotSupportedException($"Type {correctInterface.FullName } is a generic interface implemented {nameof(IRemotableAction)} which currently not suppored.");

            Logger.LogInformation($"Registering IAction class for interface of name {correctInterface.Name}");
            return correctInterface.Name ?? string.Empty;
        }

        #endregion

        #region Resolve
        public static IMapper ResolveMapper()
        {
            return Container.Resolve<IMapper>();
        }
  
        public static ILoggerFactory ResolveLoggerFactory()
        {
            return Container.Resolve<ILoggerFactory>();
        }

        public static Common.Flow.Flow ResolveFlow(string flowName = null)
        {
            if (flowName != null)
            {
                return Container.ResolveOptionalKeyed<Common.Flow.Flow>(flowName);
            }
            else
            {
                return Container.ResolveOptional<Common.Flow.Flow>();
            }
        }

        public static IBinaryMessageSerializer ResolveBinarySerializer()
        {
            return Container.ResolveOptional<IBinaryMessageSerializer>();
        }

        public static StorageServiceInfo ResolveStorage()
        {
            return Container.ResolveOptional<StorageServiceInfo>();
        }

        public static IAsyncOrchestrationFlow<Step> ResolveFlowService(string key = "default")
        {
            return Container.ResolveOptionalKeyed<IAsyncOrchestrationFlow<Step>>(key);
        }

        public static IKeyValueStorage<TKey> ResolveStorageService<TKey>()
        {
            return Container.ResolveOptional<IKeyValueStorage<TKey>>();
        }

        public static IAction ResolveAction(string actionName)
        {
            return Container.ResolveKeyed<IAction>(actionName);
        }

        public static IActorClient ResolveActorClient()
        {
            return Container.Resolve<IActorClient>();
        }

        #endregion

        #region Initialize

        public static void Build()
        {
            Container = Builder.Build();
        }

        public static void SetUpSerializer()
        {
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
        }
        public static ILifetimeScope CreateLifetimeScope(string scopeName = null)
        {
            if (!string.IsNullOrEmpty(scopeName))
            {
                return Container.BeginLifetimeScope(scopeName);
            }
            else
                return Container.BeginLifetimeScope();
        }
        #endregion
    }
}
