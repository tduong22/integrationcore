using Comvita.Common.Actor.FtpClient;
using Comvita.Common.Actor.UnifiedActor.Actions;
using Comvita.Common.Actor.UnifiedActor.Interfaces;
using Integration.Common.Flow;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.UnifiedActor.DefaultActions
{
    public class DefaultFtpSchedulerAction : BaseSchedulerAction, IDefaultFtpSchedulerAction
    {
        public DefaultFtpSchedulerAction()
        {
        }

        public async Task InvokeScheduler(FtpOption ftpOption)
        {
            var cancellationToken = CancellationToken.None;
            await StateManager.AddOrUpdateStateAsync(REMINDER_SCHEDULE, ftpOption, (s, key) => ftpOption,
                cancellationToken);
            await RegisterReminderAsync(REMINDER_SCHEDULE, null, TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(ftpOption.FtpConfig.Freq));
        }

        public override async Task StartJobAsync(DateTime timeExecuted, CancellationToken cancellationToken)
        {
            Logger.LogInformation($"{CurrentActor} SchedulerActor startjobasync");
            var ftpOption = await StateManager.GetStateAsync<FtpOption>(REMINDER_SCHEDULE, cancellationToken);
            var nextRequestContext = DefaultNextActorRequestContext;

            //every time starting a job result in a new flowinstance id
            nextRequestContext.FlowInstanceId = FlowInstanceId.NewFlowInstanceId;

            await ChainNextActorsAsync<IDefaultFtpRequestGenerateAction>(c=>c.InvokeGenerateReadRequest(ftpOption), 
                nextRequestContext, cancellationToken);
        }

        public override Task<bool> ValidateDataAsync(string actionName, object payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
