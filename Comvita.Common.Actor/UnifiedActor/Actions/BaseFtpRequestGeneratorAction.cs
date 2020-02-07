using Comvita.Common.Actor.Models;
using Comvita.Common.EventBus.Abstractions;
using Integration.Common.Actor.UnifiedActor.Actions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseFtpRequestGeneratorAction : BaseAction
    {
        protected IEventBus EventBus;

        protected BaseFtpRequestGeneratorAction(IEventBus eventBus)
            : base()
        {
            EventBus = eventBus;
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        protected virtual async Task GenerateRequestAsync(DispatchRequest requestObj)
        {
            var createdEvent = new DefaultFtpDispatchIntegrationEvent(requestObj);
            await EventBus.PublishAsync(createdEvent, requestObj.PartitionKey);
        }
    }
}
