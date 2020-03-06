using Ardalis.GuardClauses;
using Autofac;
using Comvita.Common.Actor.Utilities;
using Integration.Common.Utility.Interfaces;

namespace Comvita.Common.Actor.DI
{
    public class KeyVaultModule : Module
    {
        public string KeyVaultEndpoint {get;set; }
        public string ClientId {get;set; }
        public string ClientSecret {get;set; }

        protected override void Load(ContainerBuilder builder)
        { 
            Guard.Against.NullOrEmpty(KeyVaultEndpoint, nameof(KeyVaultEndpoint));
            builder.Register<IConfigKeyVault>(c=> new ConfigKeyVault(KeyVaultEndpoint, ClientId, ClientSecret));
        }
    }
}
