using Comvita.Common.Actor.Models;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IMessageTrackable
    {
        TrackingMessage ToTrackingMessage();
    }
}
