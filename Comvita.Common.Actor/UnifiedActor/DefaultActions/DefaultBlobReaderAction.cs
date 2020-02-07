using AutoMapper;
using Comvita.Common.Actor.Models;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Integration.Common.Actor.Clients;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultBlobReaderFtpResponseAction : BaseBlobReaderAction, IFtpResponseParserAction
    {
        public DefaultBlobReaderFtpResponseAction(BlobClient blobClient, IMapper mapper = null) : base(blobClient, mapper)
        {
        }

        public Task InvokeHandleFtpReadResponse(BlobStorageFileInfo blobStorageFileInfo)
        {
            throw new NotImplementedException();
        }

        public Task InvokeHandleFtpWriteResponse()
        {
            throw new NotImplementedException($"{nameof(DefaultBlobReaderFtpResponseAction)} not implemented InvokeHandleFtpWriteResponse. Please use another action");
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected override Task ReceiveStreamFileAsync(BlobStorageFileInfo blobInfo, Stream stream, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
