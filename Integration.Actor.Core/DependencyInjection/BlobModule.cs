using Autofac;
using Integration.Common.Actor.Clients;
using Integration.Common.Utility;
using Integration.Common.Utility.Interfaces;

namespace ServiceFabric.Integration.Actor.Core.DependencyInjection
{
    public class BlobModule : Module
    {
        public string StorageAccountKey {get;set; }
        public string StorageAccountName {get;set; }
        protected override void Load(ContainerBuilder builder) {
            
            var blobClient = new BlobClient(StorageAccountKey, StorageAccountName);
            builder.RegisterInstance(blobClient).As<BlobClient>();
            builder.RegisterType<BlobStorageConfiguration>().As<IBlobStorageConfiguration>().SingleInstance();
        }
    }
}
