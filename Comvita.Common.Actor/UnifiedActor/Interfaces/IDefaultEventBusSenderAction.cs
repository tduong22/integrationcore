using Integration.Common.Interface;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Interfaces
{
    public interface IDefaultEventBusSenderAction : IRemotableAction
    {
        Task InvokeSendIntegrationEvent(object payload);
    }
}
