using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.Models;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Comvita.Common.EventBus.Abstractions;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultFtpRequestGeneratorAction : BaseFtpRequestGeneratorAction
    {
        public const string FTP_READ_REQUEST_GENERATOR_ACTION_NAME = "FTP_READ_REQUEST_GENERATOR_ACTION_NAME";
        public const string FTP_WRITE_REQUEST_GENERATOR_ACTION_NAME = "FTP_WRITE_REQUEST_GENERATOR_ACTION_NAME";
        public const string CONNECTION_STRING_KEY = "ConnectionString";
        public const string CONTAINER_NAME_KEY = "ContainerName";
        public const string FILE_NAME_KEY = "FileName";

        public ActorIdentityWithActionName ReponseParserActor;

        private readonly IBlobStorageConfiguration _blobStorageConfiguration;
        private readonly IServiceConfiguration _serviceConfiguration;
        private readonly int _maxConnectionPool;
        protected string SectionKeyName;
        protected string FtpDispatcherActorService;

        public DefaultFtpRequestGeneratorAction(IEventBus eventBus, ActorIdentityWithActionName reponseParserActor, int maxConnectionPool = -1,
            IBlobStorageConfiguration blobStorageConfiguration = null,
            IServiceConfiguration serviceConfig = null, string ftpDispatcherActorService = null, string sectionName = null)
            : base(eventBus)
        {
            _blobStorageConfiguration = blobStorageConfiguration;
            _serviceConfiguration = serviceConfig;
            _maxConnectionPool = maxConnectionPool;

            ReponseParserActor = reponseParserActor;
            FtpDispatcherActorService = ftpDispatcherActorService;
            SectionKeyName = (string.IsNullOrEmpty(sectionName)) ? GetType().BaseType?.Name : sectionName;
        }

        private string GenerateActorId()
        {
            if (_maxConnectionPool == -1)
            {
                return Guid.NewGuid().ToString();
            }
            Random r = new Random();
            int rInt = r.Next(1, _maxConnectionPool);
            return rInt.ToString();
        }

        private async Task<IEnumerable<FtpOption>> GetFtpConfigsAsync()
        {
            var config = _serviceConfiguration.GetConfigSection(SectionKeyName);
            var configList = await _blobStorageConfiguration.GetConfigsFromBlobFile<FtpOption>(
                    config[CONNECTION_STRING_KEY],
                    config[CONTAINER_NAME_KEY],
                    config[FILE_NAME_KEY],
                    $"{SectionKeyName}");

            return configList;
        }

        public async Task InvokeGenerateReadRequest(FtpOption ftpOption)
        {
            var ftpDispatchRequest = new DispatchRequest(ftpOption, typeof(FtpOption), ftpOption?.Domain, ReponseParserActor, null);

            //if using sb
            //await GenerateRequestAsync(ftpDispatchEvent);

            // testing only
            var newExecutableOrder = new ExecutableOrchestrationOrder()
            {
                ActorId = GenerateActorId(),
                ActorServiceUri = (FtpDispatcherActorService != null) ? $"{ApplicationName}/{FtpDispatcherActorService}" : ServiceUri.ToString()
            };
            await ChainNextActorsAsync<IDefaultFtpDispatchAction>(
                c => c.InvokeDispatchReadRequest(ftpDispatchRequest), 
                new ActorRequestContext(Id.ToString(),
                "FTP_READ_DISPATCH_ACTION_NAME",
                Guid.NewGuid().ToString(), CurrentFlowInstanceId), newExecutableOrder, CancellationToken.None);
        }

        public async Task InvokeGenerateWriteRequest(object content)
        {
            if (content is ICsvContent csvContent)
            {
                var csvData = csvContent.ToCsv();
                var ftpOption = (await GetFtpConfigsAsync()).ToList().First();
                var ftpWriterOption = new FtpWriterOption(ftpOption.FtpConfig, csvData.Item1, csvData.Item2);
                var ftpDispatchRequest = new DispatchRequest(ftpWriterOption, typeof(FtpWriterOption), null, ReponseParserActor,
                    null);
                // for dispatcher if need to send events
                //await GenerateRequestAsync(ftpDispatchEvent);

                // testing only
                var newExecutableOrder = new ExecutableOrchestrationOrder()
                {
                    ActorId = GenerateActorId(),
                    ActorServiceUri = (FtpDispatcherActorService != null) ? $"{ApplicationName}/{FtpDispatcherActorService}" : ServiceUri.ToString()
                };

                await ChainNextActorsAsync<IDefaultFtpDispatchAction>(c => c.InvokeDispatchWriteRequest(ftpDispatchRequest), new ActorRequestContext(Id.ToString(), "FTP_WRITE_DISPATCH_ACTION_NAME",
                    Guid.NewGuid().ToString(), CurrentFlowInstanceId), newExecutableOrder, CancellationToken.None);
            }
            else
            {
                throw new NotImplementedException($"{CurrentActor} failed to generate content to write to ftp. Interface {nameof(ICsvContent)} or {nameof(IXmlContent)} not implemented on the entity. ");
            }
        }
    }
}