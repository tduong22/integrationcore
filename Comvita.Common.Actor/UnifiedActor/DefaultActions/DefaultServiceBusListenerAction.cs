using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.EventBus.Abstractions;
using Comvita.Common.EventBus.EventBusOption;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultServiceBusListenerAction : BaseEventBusListenerAction
    {
        protected IDynamicIntegrationEventHandler Handler;
        protected readonly List<string> DynamicEventConst = new List<string>();
        public const string DEFAULT_INIT_SCHEDULER_ACTION_NAME = "DEFAULT_INIT_SCHEDULER_ACTION_NAME";

        public DefaultServiceBusListenerAction(IDynamicIntegrationEventHandler handler, IEnumerable<string> dynamicEventConst = null)
             : base()
        {
            Handler = handler;
            if (dynamicEventConst != null) DynamicEventConst = dynamicEventConst.ToList();
        }

        public override async Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload, CancellationToken cancellationToken)
        {
           var serviceBusOption = Deserialize<ServiceBusOption>(payload);
           await ChainCreateEventBusListenersAsync(serviceBusOption);
        }

        protected override async Task Subscribe()
        {
            foreach (var eventName in DynamicEventConst)
            {
                try
                {
                    await EventBus.SubscribeDynamicAsync(eventName, Handler);
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex, $"{CurrentActor} failed to subscribe event {eventName} {ex.Message}");
                }
            }
        }

        protected override async Task UnSubscribe()
        {
            foreach (var eventName in DynamicEventConst)
            {
                try
                {
                    await EventBus.UnSubscribeDynamicAsync(eventName, Handler);
                }
                catch (System.Exception ex)
                {
                    Logger.LogError(ex, $"{CurrentActor} failed to unsubscribe event {eventName} {ex.Message}");
                }
            }
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
