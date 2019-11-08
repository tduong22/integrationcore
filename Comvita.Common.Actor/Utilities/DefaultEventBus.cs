using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comvita.Common.Actor.Events;
using Comvita.Common.EventBus.Abstractions;
using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Utilities
{
    public class DefaultEventBus : IEventBus
    {
        public const string DEFAULT_EVENT_BUS_MESSAGE = "The default IEventBus is in use because of there are exceptions with registering and resolving the correct instance";
        public async Task PublishAsync(Event @event, IDictionary<string, string> customProperties = null)
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task PublishAsync(Event @event, string partitionKey, IDictionary<string, string> customProperties = null)
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task SubscribeAsync<T, TH>() where T : Event where TH : IIntegrationEventHandler<T>
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task SubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task SubscribeDynamicAsync<TH>(List<string> eventNamesList) where TH : IDynamicIntegrationEventHandler
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task UnsubscribeDynamicAsync<TH>(string eventName) where TH : IDynamicIntegrationEventHandler
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task UnsubscribeAsync<T, TH>() where T : Event where TH : IIntegrationEventHandler<T>
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task SubscribeDynamicAsync(string eventName, IDynamicIntegrationEventHandler eventHandler)
        {
             Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task SubscribeDynamicAsync(List<string> eventNameList, IDynamicIntegrationEventHandler eventHandler)
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task UnSubscribeDynamicAsync(string eventName, IDynamicIntegrationEventHandler eventHandler)
        {
             Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }

        public async Task InitializeEventBus()
        {
            Console.WriteLine(DEFAULT_EVENT_BUS_MESSAGE);
        }
    }
}
