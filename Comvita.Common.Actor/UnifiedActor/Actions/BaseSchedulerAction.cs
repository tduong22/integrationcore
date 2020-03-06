using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseSchedulerAction : BaseAction, IActivatableAction, IRemindableAction
    {
        protected IActorReminder ActorReminder;
        protected const string REMINDER_SCHEDULE = "REMINDER_SCHEDULE";

        protected BaseSchedulerAction() : base()
        {
        }

        public async Task OnActivateAsync()
        {
            //fix an issue when manually overwriting the reminder with custom interval being reset because actor is balancing between nodes
            var isDefaultReminder = (((await StateManager.GetStateNamesAsync()).FirstOrDefault(x => x == REMINDER_SCHEDULE)) == null);

            if (isDefaultReminder)
            {
                ActorReminder = await RegisterReminderAsync(REMINDER_SCHEDULE, null, TimeSpan.FromSeconds(5), TimeSpan.FromMinutes(1));
            }
        }

        public Task OnDeactivateAsync()
        {
            Logger.LogInformation($"{CurrentActor} is being deactivated...");
            return Task.CompletedTask;
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(REMINDER_SCHEDULE))
            {
                var cancellationToken = CancellationToken.None;
                try
                {
                    await StartJobAsync(DateTime.UtcNow, cancellationToken);
                    await OnSuccessAsync(cancellationToken);
                }
                catch (Exception ex)
                {
                    await OnFailedAsync(ex, cancellationToken);
                    throw;
                }
            }
        }

        public abstract Task StartJobAsync(DateTime timeExecuted, CancellationToken cancellationToken);

        #region Virtual Methods that should be implemented

        public virtual Task OnSuccessAsync(CancellationToken cancellationToken)
        {
            //log successful scheduler
            return Task.CompletedTask;
        }

        public virtual Task OnFailedAsync(Exception ex, CancellationToken cancellationToken)
        {
            //log failed scheduler
            return Task.CompletedTask;
        }
        #endregion
    }
}
