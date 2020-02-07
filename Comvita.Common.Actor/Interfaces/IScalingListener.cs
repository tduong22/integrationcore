using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IScalingListener
    {
        Task RegisterListener(Func<Task> callBackFunction, CancellationToken cancellationToken);
        Task StopListener();
    }
}