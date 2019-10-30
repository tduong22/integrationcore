using Polly;
using Polly.Registry;

namespace Integration.Common.Actor.Interface
{
    public interface IPolicyRegistry
    {
        PolicyRegistry CreateRegistry();
        Policy GetPolicy(string key);
    }
}
