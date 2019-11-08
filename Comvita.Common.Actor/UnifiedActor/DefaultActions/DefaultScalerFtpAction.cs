using Comvita.Common.Actor.Extensions;
using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Integration.Common.Actor.Clients;
using Integration.Common.Actor.Helpers;
using Integration.Common.Model;
using Integration.Common.Utility.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultFtpScalerAction : BaseSchedulerAction, IDefaultFtpScalerAction
    {
        protected string SchedulerActorService;
        protected IServiceConfiguration ServiceConfiguration;
        protected const string FTP_LIST_STATE_NAME = "FTP_LIST_STATE_NAME";
        protected const int FREQ = 10;
        protected string SectionName;
        public DefaultFtpScalerAction(string schedulerActorService, IServiceConfiguration serviceConfiguration, string sectionName) : base()
        {
            SchedulerActorService = schedulerActorService;
            ServiceConfiguration = serviceConfiguration;
            SectionName = sectionName;
        }

        public async Task InvokeFtpScaler()
        {
            await RegisterReminderAsync(REMINDER_SCHEDULE, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(FREQ));
        }

        public override async Task StartJobAsync(DateTime timeExecuted, CancellationToken cancellationToken)
        {
            try
            {
                Logger.LogInformation($"ScalerActor startjobasync");
                var ftpOptions =
                    await StateManager.TryGetStateAsync<List<FtpOption>>(FTP_LIST_STATE_NAME, cancellationToken);
                var configs = ServiceConfiguration.GetConfigSection("BlobStorageForFtp");
                var blobFtpList = await ReadFtpConfigAsync(configs);
                if (ftpOptions.HasValue)
                {
                    var stateFtpList = ftpOptions.Value;
                    bool listChanged = false;
                    if (stateFtpList.Count == blobFtpList.Count)
                    {
                        foreach (var ftpOption in stateFtpList)
                        {
                            listChanged = blobFtpList.Any(item =>
                            {
                                string id = item.FtpConfig.Ftp.Host + item.FtpConfig.Ftp.Path;
                                if (id == ftpOption.FtpConfig.Ftp.Host + ftpOption.FtpConfig.Ftp.Path)
                                {
                                    string ftpOptionString = ftpOption.Serialize();
                                    string itemString = item.Serialize();
                                    return !ftpOptionString.Equals(itemString);
                                }

                                return true;
                            });
                            if (listChanged) break;
                        }
                    }
                    else
                    {
                        listChanged = true;
                    }

                    if (listChanged)
                    {
                        var intersectList = blobFtpList.Intersect(stateFtpList, new FtpOptionComparer()).ToList();
                        var remainingList = stateFtpList.Except(intersectList, new FtpOptionComparer()).ToList();

                        foreach (var ftpOption in remainingList)
                        {
                            string actorId = ftpOption.FtpConfig.Ftp.Host + ftpOption.FtpConfig.Ftp.Path + ftpOption.FtpConfig.Freq;
                            DisposeActor(new ActorId(actorId), new Uri($"{ApplicationName}/{SchedulerActorService}"), cancellationToken);
                        }
                        await StateManager.AddOrUpdateStateAsync(FTP_LIST_STATE_NAME, blobFtpList, (s, key) => blobFtpList,
                            cancellationToken);
                        foreach (var ftpOption in blobFtpList)
                        {
                            await InitSchedulerActor(ftpOption, cancellationToken);
                        }
                    }
                }
                else
                {
                    await StateManager.AddOrUpdateStateAsync(FTP_LIST_STATE_NAME, blobFtpList, (s, key) => blobFtpList,
                        cancellationToken);
                    foreach (var ftpOption in blobFtpList)
                    {
                        await InitSchedulerActor(ftpOption, cancellationToken);
                    }
                }

            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to start job. Message: {ex.Message}");
            }
        }

        private async Task<List<FtpOption>> ReadFtpConfigAsync(IDictionary<string, string> configs)
        {
            try
            {
                var config = ServiceConfiguration.GetConfigSection("BlobStorageForFtp");
                var blobClient = new BlobClient(config["StorageAccountKey"], config["StorageAccountName"]);
                var ftpActorListStream = await blobClient.ReadBlobAsync(configs["ContainerName"], configs["FileName"]);
                var ftpOntionList = new List<FtpOption>();
                using (var reader = new StreamReader(ftpActorListStream))
                {
                    string content = reader.ReadToEnd();
                    if (!string.IsNullOrEmpty(content))
                    {
                        var actorObject = JObject.Parse(content);
                        var arrayActor = actorObject[SectionName].Values<JObject>();
                        foreach (var actor in arrayActor)
                        {
                            var ftpConfig = actor.ToObject<FtpOption>();
                            ftpOntionList.Add(ftpConfig);
                        }
                    }
                }
                return ftpOntionList;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to read ftp config from blob storage with storage name: {configs["StorageAccountName"]} and container name: {configs["ContainerName"]}. Message: {ex.Message}");
                throw;
            }
        }
        private async Task InitSchedulerActor(FtpOption ftpOption, CancellationToken cancellationToken)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var actorId = ftpOption.FtpConfig.Ftp.Host + ftpOption.FtpConfig.Ftp.Path + ftpOption.FtpConfig.Freq;
                await ChainNextActorsAsync<IDefaultFtpSchedulerAction>(c=>c.InvokeScheduler(ftpOption), DefaultNextActorRequestContext, new OrchestrationOrder($"{ApplicationName}/{SchedulerActorService}", actorId).ToExecutableOrder(), cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"{CurrentActor} failed to init scheduler actor: " + ex.Message, ftpOption);
                throw;
            }
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

    }
}
