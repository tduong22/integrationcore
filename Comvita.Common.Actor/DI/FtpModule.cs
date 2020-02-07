using Ardalis.GuardClauses;
using Autofac;
using Comvita.Common.Actor.DI;
using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.Utilities;
using Integration.Common.Utility.Interfaces;

namespace Comvita.Common.Actor.DependencyInjection
{
    public enum FtpProtocol
    {
        FTP = 0,
        FTPS = 10,
        SFTP = 20
    }
    public class FtpModule : Module
    {
        public FtpProtocol FtpProtocol { get; set; }
        public bool PasswordInKeyvault { get; set; } = true;
        //public string StorageAccountKey { get; set; }
        //public string StorageAccountName { get; set; }

        public string KeyVaultEndpoint {get;set; }
        public string ClientId {get;set; }
        public string ClientSecret {get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            if (FtpProtocol == FtpProtocol.SFTP)
            {
                builder.RegisterType<ActorSftpClient>().As<IActorFtpClient>();
            }
            else
            {
                builder.RegisterType<ActorFtpClient>().As<IActorFtpClient>();
            }
            builder.RegisterType<FtpPolicyRegistry>().As<IFtpPolicyRegistry>().SingleInstance()
                   .OnActivated(c => c.Instance.CreateRegistry());
            builder.RegisterType<FtpStorage>().As<IFtpStorage>();
            
            if (PasswordInKeyvault) {
                Guard.Against.NullOrEmpty(KeyVaultEndpoint, nameof(KeyVaultEndpoint));
                builder.RegisterModule(new KeyVaultModule() { 
                KeyVaultEndpoint = KeyVaultEndpoint,
                ClientId = ClientId,
                ClientSecret = ClientSecret
            });
            }
        }
    }
}
