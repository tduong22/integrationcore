using Autofac;
using Comvita.Common.Actor.Events;
using System.Collections.Generic;

namespace Comvita.Common.Actor.DependencyInjection
{
    public class EventManagerModule : Module
    {
        public List<IEventPersister> EventPersisters {get;set; }
        public EventManagerModule()
        {

        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<DefaultEventManager>().As<IEventManager>().SingleInstance().OnActivated(c =>
                EventPersisters.ForEach(i => c.Instance.AddEventPersisters(i)));
        }
    }
}
