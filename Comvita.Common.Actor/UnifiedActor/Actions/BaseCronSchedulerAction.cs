using Comvita.Common.Actor.Models;
using Integration.Common.Actor.UnifiedActor.Actions;
using Integration.Common.Interface;
using Microsoft.Extensions.Logging;
using Quartz;
using Quartz.Impl.Matchers;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.Actions
{
    public abstract class BaseCronSchedulerAction : BaseAction, IActivatableAction, IRemindableAction
    {
        protected const string STAY_ALIVE_REMINDER_NAME = "STAY_ALIVE_REMINDER_NAME";
        protected TimeSpan ReminderTimeSpan = TimeSpan.FromMinutes(10);
        protected IScheduler Scheduler;

        protected const string _SCHEDULER_STATE_NAME = "SCHEDULER_STATE_NAME";
        protected const string CRON_STATE_NAME = "CRON_STATE_NAME";
        protected readonly Type TypeOfJob;


        protected Type TypeofJob { get; }

        protected BaseCronSchedulerAction(IScheduler scheduler, Type typeofJob) : base()
        {
            Scheduler = scheduler ?? throw new ArgumentNullException(nameof(IScheduler));
            TypeofJob = typeofJob;
        }

        public async Task OnActivateAsync()
        {
            await RegisterReminderAsync(STAY_ALIVE_REMINDER_NAME, null, TimeSpan.Zero, ReminderTimeSpan);
            var config = await StateManager.TryGetStateAsync<CronSchedulerConfig>(CRON_STATE_NAME);
            if (config.HasValue)
            {
                var storedConfig = config.Value;
                await StartCronAsync(TypeofJob, storedConfig.Id, storedConfig.CronExpression);
            }
        }

        public async Task OnDeactivateAsync()
        {
            await StopCronAsync();
        }

        protected async Task StartCronAsync(Type typeOfJob, string cronId, string cronExpression)
        {
            try
            {
                var isValid = CronExpression.IsValidExpression(cronExpression);
                if (isValid)
                {
                    if ((await Scheduler.GetJobDetail(new JobKey(cronId)) == null))
                    {
                        var jobData = await StateManager.GetStateAsync<CronSchedulerConfig>(CRON_STATE_NAME);
                        var jobDataMap = new JobDataMap(new Dictionary<string, CronSchedulerConfig>() { { cronId, jobData } });
                        IJobDetail jobDetail = JobBuilder.Create(typeOfJob) //careful with time zone
                         .WithIdentity(cronId)
                         .UsingJobData(jobDataMap)
                         .Build();

                        var trigger = TriggerBuilder.Create()
                             .ForJob(jobDetail)
                             .WithCronSchedule(cronExpression, x => x.InTimeZone(TimeZoneInfo.Utc))
                             .WithIdentity(cronId)
                             .StartNow()
                             .Build();

                        await Scheduler.Start();
                        await Scheduler.ScheduleJob(jobDetail, trigger);
                    }
                }
                else throw new ArgumentException($"cronExpression {cronExpression} is not valid");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[StartCronAsync] Failed to start cron job: " + ex.Message);
                throw;
            }
        }

        protected async Task StopCronAsync()
        {
            var jobKeys = await Scheduler.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            foreach (var jobKey in jobKeys)
            {
                await Scheduler.DeleteJob(jobKey);
            }
        }

        protected Task StartCronAsync<TJob>(string cronId, string cronExpression) where TJob : IJob
        {
            return StartCronAsync(typeof(TJob), cronId, cronExpression);
        }

        public Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            Logger.LogInformation($"{CurrentActor} CronSchedulerAction Still Alive at {DateTime.UtcNow}");
            return Task.CompletedTask;
        }
        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
