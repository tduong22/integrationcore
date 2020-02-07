using Ardalis.GuardClauses;
using Autofac;
using Comvita.Common.EventBus.Abstractions;
using Comvita.Common.EventBus.EventBusOption;
using Comvita.Common.EventBus.ServiceBus;
using Microsoft.Azure.ServiceBus;
using System.Collections.Generic;

namespace Comvita.Common.Actor.DependencyInjection
{
    public class EventBusSenderModule : Module
    {
        public List<KeyValuePair<string, string>> ServiceBusConnectionStrings { get; set; }
        public bool isCompressed { get; set; } = false;

        protected override void Load(ContainerBuilder builder)
        {
            Guard.Against.Null(ServiceBusConnectionStrings, nameof(ServiceBusConnectionStrings));
            foreach (var connString in ServiceBusConnectionStrings)
            {
                Guard.Against.NullOrEmpty(connString.Value, nameof(connString));
                var serviceBusOption = new ServiceBusOption()
                {
                    ConnectionString = connString.Value,
                    ClientMode = ClientMode.Sending,
                    IsCompressed = isCompressed
                };

                if (ServiceBusConnectionStrings.Count == 1)
                {
                    builder.Register(c =>
                    {
                        var serviceBusConnection = new ServiceBusConnectionStringBuilder(connString.Value);
                        var serviceBusPersisterConnection = new DefaultServiceBusPersisterConnection(serviceBusConnection);
                        return new EventBusServiceBus(serviceBusPersisterConnection, serviceBusOption);
                    }).As<IEventBus>().SingleInstance();
                }
                else
                {
                    builder.Register(c =>
                    {
                        var serviceBusConnection = new ServiceBusConnectionStringBuilder(connString.Value);
                        var serviceBusPersisterConnection = new DefaultServiceBusPersisterConnection(serviceBusConnection);
                        return new EventBusServiceBus(serviceBusPersisterConnection, serviceBusOption);
                    }).Keyed<IEventBus>(connString.Key).SingleInstance();
                }
            }
        }
    }
}

