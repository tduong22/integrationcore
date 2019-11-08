using Comvita.Common.Actor.Events;
using Comvita.Common.EventBus.Abstractions;
using Comvita.Common.EventBus.Events;
using Integration.Common.Actor.UnifiedActor.Actions;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseEventBusSenderAction : BaseAction
    {
        protected IEventBus EventBus;
        protected BaseEventBusSenderAction(IEventBus eventBus)
            : base()
        {
            EventBus = eventBus;
        }

        public async Task PublishAsync(Event @event, string partitionKey, IDictionary<string, string> customProperties = null)
        {
            //set correlationId
            @event.CorrelationId = new CorrelationId(CurrentFlowInstanceId?.Id);

            Logger.LogInformation($"{CurrentActor} is going to publish event with correlation id {@event.CorrelationId} with partition key {partitionKey}");
            await EventBus.PublishAsync(@event, partitionKey, customProperties);
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
