using Polly;
using Polly.Registry;

namespace Comvita.Common.Actor.Interfaces
{
    public interface IPolicyRegistry
    {
        PolicyRegistry CreateRegistry();
        Policy GetPolicy(string key);
    }
}
