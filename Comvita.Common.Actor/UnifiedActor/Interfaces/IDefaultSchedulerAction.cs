using Comvita.Common.Actor.FtpClient;
using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IDefaultFtpSchedulerAction : IRemotableAction
    {
        Task InvokeScheduler(FtpOption ftpOption);
    }
}
