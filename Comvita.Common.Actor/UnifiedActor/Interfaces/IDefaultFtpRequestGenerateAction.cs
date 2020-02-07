using Comvita.Common.Actor.FtpClient;
using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IDefaultFtpRequestGenerateAction : IRemotableAction
    {
        Task InvokeGenerateReadRequest(FtpOption ftpOption);
        Task InvokeGenerateWriteRequest(object content);
    }
}
