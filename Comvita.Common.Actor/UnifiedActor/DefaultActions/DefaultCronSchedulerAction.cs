namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{/*
    public class DefaultCronSchedulerAction : BaseCronSchedulerAction
    {   
        private const string _SCHEDULER_STATE_NAME = "SCHEDULER_STATE_NAME";
        public DefaultCronSchedulerAction(IScheduler scheduler) : base(scheduler)
        {
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload,
            CancellationToken cancellationToken = new CancellationToken()) => Task.FromResult(true);

        public override async Task ChainProcessMessageAsync(ActorRequestContext actorRequestContext, byte[] payload,
            CancellationToken cancellationToken)
        {
            var schedulerConfig =  DeserializePayload<SchedulerConfig>(payload);
            var schedulerState = await StateManager.TryGetStateAsync<SchedulerConfig>(_SCHEDULER_STATE_NAME);
            if (!schedulerState.HasValue)
            {
                await StateManager.AddOrUpdateStateAsync(_SCHEDULER_STATE_NAME, schedulerConfig,
                    (s, key) => schedulerConfig, cancellationToken);
                //use for testing Cron Expression: 0 0/1 * * * ?
                await StartCronAsync<DailyJob>(schedulerConfig.CronExpression);
            }
        }
        /*
        public override async Task OnActivateAsync()
        {
            await base.OnActivateAsync();

            if (JobDetail == null)
            {
                var schedulerOption = await StateManager.TryGetStateAsync<SchedulerConfig>(_SCHEDULER_STATE_NAME);
                if (schedulerOption.HasValue)
                {
                    await StartCronAsync<DailyJob>(schedulerOption.Value.CronExpression);
                }
            }
        }
        protected override async Task OnDeactivateAsync()
        {
            await base.OnDeactivateAsync();
            await Scheduler.DeleteJob(JobDetail.Key);
        }
        
        public override Task<MessageObjectResult> InternalProcessAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new System.NotImplementedException();
        }
    }
    */
}
