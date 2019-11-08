using Comvita.Common.Actor.Interfaces;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Comvita.Common.Actor.Utilities;
using Comvita.Common.EventBus.Abstractions;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultServiceBusSenderAction : BaseEventBusSenderAction, IDefaultEventBusSenderAction
    {
        public const string SENDING_TO_EVENT_BUS_ACTION_NAME = "SENDING_TO_EVENT_BUS_ACTION_NAME";
        public string DYNAMIC_EVENT_NAME_CONST;

        protected Type EventType;
        protected string DefaultPartitionKey = null;

        public DefaultServiceBusSenderAction(IEventBus eventBus, Type eventType, string defaultPartitionKey) : base(eventBus)
        {
            DYNAMIC_EVENT_NAME_CONST = eventType.Name;
            EventType = eventType;
            DefaultPartitionKey = defaultPartitionKey;
        }
        public async Task InvokeSendIntegrationEvent(object payload)
        {
            var newEvent = InstanceUtilities.CreateEventInstance(payload, EventType);
            string partitionKey = DefaultPartitionKey;
            if (payload is IPartitionable partitionable)
            {
                partitionKey = partitionable.ExtractPartitionKey();
                if (string.IsNullOrEmpty(partitionKey))
                {
                    partitionKey = DefaultPartitionKey;
                }
            }
            await PublishAsync(newEvent, partitionKey);
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }
}
