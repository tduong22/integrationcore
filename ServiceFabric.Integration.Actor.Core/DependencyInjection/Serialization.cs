using Autofac;
using Integration.Common.Interface;
using MessagePack;
using ServiceFabric.Integration.Actor.Core.Serialization;

namespace Integration.Common.Actor.DependencyInjection
{
    public class SerializationModule : Module
    {
        protected override void Load(ContainerBuilder builder) {
            MessagePackSerializer.SetDefaultResolver(MessagePack.Resolvers.ContractlessStandardResolver.Instance);
            builder.RegisterType<MessagePackBinaryMessageSerializer>().As<IBinaryMessageSerializer>().SingleInstance();
        }
    }
}
