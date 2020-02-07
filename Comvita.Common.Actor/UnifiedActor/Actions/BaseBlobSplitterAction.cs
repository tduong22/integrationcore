using Comvita.Common.Actor.Constants;
using Comvita.Common.Actor.Events;
using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.Models;
using Comvita.Common.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseBlobSplitterAction : BaseSplitterAction
    {
        private readonly IBlobStorageService _blobStorageService;

        private readonly IEventBus _eventBus;

        protected abstract string FileNamePrefix { get; }

        protected abstract string ContainerName { get; set; }

        protected BaseBlobSplitterAction(IBlobStorageService blobStorageService, IEventBus eventBus)
             : base()
        {
            _blobStorageService = blobStorageService;
            _eventBus = eventBus;
        }

        protected async Task<SplitResult<T>> Split<T>(IEnumerable<T> listOfdata, Func<T, CancellationToken, Task<bool>> funcProcess, AggregateBlobIntegrationEvent aggregateBlobIntegrationEvent, CancellationToken cancellationToken)
        {
            try
            {
                var data = JsonConvert.SerializeObject(listOfdata);
                var isSuccess = await WriteFileToBlobAsync(data, aggregateBlobIntegrationEvent.BlobStorageFileInfo.FileName);
                string domain = CommonConstants.DEFAULT_DOMAIN;

                if (listOfdata.First() is IPartitionable firstItem)
                {
                    domain = firstItem.ExtractPartitionKey();
                }

                await _eventBus.PublishAsync(aggregateBlobIntegrationEvent, domain);
                return await base.Split(listOfdata, funcProcess, cancellationToken);
            }
            catch (Exception e)
            {
                Logger.LogError($"[BaseBlobSplitterAction] Failed to split data {e}");
                throw;
            }
        }

        private async Task<bool> WriteFileToBlobAsync(string data, string fileName)
        {
            try
            {
                await _blobStorageService.CreateFileAsync(fileName, Encoding.UTF8.GetBytes(data));
                return true;
            }
            catch (Exception e)
            {
                Logger.LogError($"[BaseBlobSplitterAction] Failed to write file to blobStorage {e}");
                throw;
            }
        }
    }
}