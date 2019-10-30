using Integration.Common.Actor.Interface;
using Polly;
using Polly.NoOp;
using Polly.Registry;

namespace Integration.Common.Model
{
    public abstract class GeneralPolicyRegistry : IPolicyRegistry
    {
        public static int ConnectionTimeOutInSeconds = 60;

        public static PolicyRegistry Registry;

        static GeneralPolicyRegistry()
        {
            Registry = new PolicyRegistry();
        }

        public abstract PolicyRegistry CreateRegistry();
        public virtual Policy GetPolicy(string key)
        {
            var policyExists = Registry.ContainsKey(key);
            if (!policyExists)
            {
                // if policy not exist return noop policy for passing through policy
                NoOpPolicy noOp = Policy.NoOp();
                return noOp;
            }
            return Registry.Get<Policy>(key);
        }
    }
}
