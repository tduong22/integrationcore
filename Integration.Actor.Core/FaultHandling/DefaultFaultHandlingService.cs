using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.Utilities;
using Integration.Common.Flow;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using ServiceFabric.Integration.Actor.Core.Loggings;
using ServiceFabric.Integration.Actor.Core.Models;

namespace ServiceFabric.Integration.Actor.Core.FaultHandling
{
    public class DefaultFaultHandlingService : IFaultHandlingService
    {
        public ILogger Logger;
        public const string REMINDER_NAME = "ReminderName";
        public DefaultFaultHandlingService(ILoggerFactory loggerFactory)
        {
            Logger = loggerFactory.CreateLogger<DefaultFaultHandlingService>();
        }
        public async Task HandleFaultAsync(string actionName, object payload, Exception exception, ActorExecutionContext actorExecutionContext, CancellationToken cancellationToken)
        {
            try
            {
                if (exception is ActorMessageValidationException)
                {
                    Logger.LogError(exception, $"{actorExecutionContext.ActionName} failed to validate the message by {actionName}");
                }
                else
                {
                    var dict = LoggingUtilities.CreateLoggingDictionary(actorExecutionContext);
                    LoggingUtilities.LogPayload(Logger, payload, actionName, dict, exception);
                }
               
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, $"[OnFailedAsync] {actorExecutionContext.ActionName + " " + actorExecutionContext.FlowName} failed to chain error handling process message of the action name {actionName}. Message: {ex.Message}");
            }
        }
        
        
    }
}
