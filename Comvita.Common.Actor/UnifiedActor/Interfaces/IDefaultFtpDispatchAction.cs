using Comvita.Common.Actor.Models;
using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IDefaultFtpDispatchAction : IRemotableAction
    {
        Task InvokeDispatchReadRequest(DispatchRequest requestRead);
        Task InvokeDispatchWriteRequest(DispatchRequest requestWrite);
    }
}
