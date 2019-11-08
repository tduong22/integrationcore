using Autofac;
using Comvita.Common.EventBus;
using Comvita.Common.EventBus.Abstractions;
using Comvita.Common.EventBus.EventBusOption;
using Comvita.Common.EventBus.ServiceBus;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Interface;
using Microsoft.Azure.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors.Runtime;
using ServiceFabric.Integration.Actor.Core.Helpers;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseEventBusListenerAction : BaseAction, IActivatableAction, IRemindableAction
    {
        protected IEventBus EventBus;
        private readonly string BASE_EB_STATE_NAME = "ServiceBusOptionState";
        protected IActorReminder ActorReminder;
        protected const string REMINDER_SCHEDULE = "REMINDER_SCHEDULE";

        protected BaseEventBusListenerAction()
            : base()
        {
        }

        public async Task OnActivateAsync()
        {
            if (EventBus == null)
            {
                var serviceBusOption = await StateManager.TryGetStateAsync<ServiceBusOption>(BASE_EB_STATE_NAME);
                if (serviceBusOption.HasValue)
                {
                    await InitEventBusClient(serviceBusOption.Value);
                }
            }
        }

        public async Task OnDeactivateAsync()
        {
            Logger.LogInformation($"{CurrentActor} BaseEventBusListenerAction is being deactivated....Unsubscribing event listener");
            await UnSubscribe();
        }


        protected async Task ChainCreateEventBusListenersAsync(ServiceBusOption serviceBusOption)
        {
            await StateManager.AddOrUpdateStateAsync(BASE_EB_STATE_NAME, serviceBusOption, (key, value) => serviceBusOption);
            await InitEventBusClient(serviceBusOption);
        }

        public virtual Task StartJobAsync(DateTime timeExecuted, CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }

        protected virtual async Task InitEventBusClient(ServiceBusOption serviceBusOption)
        {
            try
            {
                //retrieve from servicebus option             
                var serviceBusConnection = new ServiceBusConnectionStringBuilder(serviceBusOption.ConnectionString);
                var serviceBusPersisterConnection = new DefaultServiceBusPersisterConnection(serviceBusConnection);
                var eventBusSubcriptionsManager = new InMemoryEventBusSubscriptionsManager();

                //resolve if needed using the scope
                //careful consider create child scope instead of passing container
                EventBus = new EventBusServiceBus(serviceBusPersisterConnection, eventBusSubcriptionsManager, serviceBusOption.SubscriptionName, CoreDependencyResolver.Container, serviceBusOption, CoreDependencyResolver.Container.Resolve<ILoggerFactory>());
                await ((EventBusServiceBus)EventBus).InitializeAsync(serviceBusPersisterConnection, eventBusSubcriptionsManager,
                    serviceBusOption.SubscriptionName, RetryPolicy.Default);
                await Subscribe();
            }
            catch (Exception ex)
            {
                Logger.LogError($"{CurrentActor} failed to init service bus client with provided service bus option subscriptionName = {serviceBusOption.SubscriptionName}", ex);
                throw;
            }
        }

        #region EventBus

        protected abstract Task Subscribe();

        protected abstract Task UnSubscribe();

        public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            return Task.CompletedTask;
        }

        #endregion
    }
}
