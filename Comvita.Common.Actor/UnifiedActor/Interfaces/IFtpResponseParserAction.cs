using Comvita.Common.Actor.Models;
using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IFtpResponseParserAction : IRemotableAction
    {
        Task InvokeHandleFtpReadResponse(BlobStorageFileInfo blobStorageFileInfo);

        Task InvokeHandleFtpWriteResponse();
    }
}
