using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting;

[assembly: FabricTransportActorRemotingProvider(RemotingListenerVersion = RemotingListenerVersion.V2_1, RemotingClientVersion = RemotingClientVersion.V2_1)]
namespace Comvita.Common.Actor.Interfaces
{
    public interface IBaseMessagingActor : IActor
    {
        Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload,
            CancellationToken cancellationToken);
    }
}
