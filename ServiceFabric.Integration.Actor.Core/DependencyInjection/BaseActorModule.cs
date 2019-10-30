using Autofac;
using Integration.Common.Actor.Clients;
using Integration.Common.Actor.Interface;
using Integration.Common.Actor.Persistences;

namespace Integration.Actor.Core.DependencyInjection
{
    public class BaseActorModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            //register actor request persistence
            builder.RegisterType<ActorRequestPersistence>().As<IActorRequestPersistence>();

            //register actorclient
            builder.RegisterType<MessagingActorClient>().As<IActorClient>();
        }
    }
}
