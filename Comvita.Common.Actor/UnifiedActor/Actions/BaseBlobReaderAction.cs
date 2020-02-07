using AutoMapper;
using Comvita.Common.Actor.Models;
using Integration.Common.Actor.Clients;
using Integration.Common.Actor.UnifiedActor.Actions;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseBlobReaderAction : BaseAction
    {
        protected BlobClient BlobClient;
        protected IMapper Mapper;
        protected BaseBlobReaderAction(BlobClient blobClient, IMapper mapper = null) : base()
        {
            BlobClient = blobClient;
            Mapper = mapper;
        }

        protected async Task ReadBlobAsync(BlobStorageFileInfo blobInfo, CancellationToken cancellationToken)
        {
            try
            {
                var streamFile = await BlobClient.ReadBlobAsync(blobInfo.Container, blobInfo.FileName);
                await ReceiveStreamFileAsync(blobInfo, streamFile, cancellationToken);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[ReadBlobAsync] {CurrentActor} Failed to read Blob: " + ex.Message);
            }

        }
        protected abstract Task ReceiveStreamFileAsync(BlobStorageFileInfo blobInfo, Stream stream, CancellationToken cancellationToken);
    }
}
