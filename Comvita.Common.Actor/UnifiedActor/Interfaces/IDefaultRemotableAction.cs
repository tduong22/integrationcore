using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IDefaultRemotableAction : IRemindableAction
    {
        Task InvokeDefaultMethod(object payload);
    }
}
