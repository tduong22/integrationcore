using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using ServiceFabric.Integration.Actor.Core.Models;

namespace ServiceFabric.Integration.Actor.Core.Loggings
{
    public static class LoggingUtilities
    {
        public static Dictionary<string, object> CreateLoggingDictionary(ActorExecutionContext actorExecutionContext)
        {
            var dict = new Dictionary<string, object>{
                    {"ServiceUri", actorExecutionContext.ServiceUri},
                    {"Payload", actorExecutionContext.Payload},
                    {"MethodName", actorExecutionContext.MethodName },
                    {"ActionName", actorExecutionContext},
                    {"Actor", actorExecutionContext.ActionName},
                    {"ReminderName", actorExecutionContext.ReminderName},
                    {"OperationId", actorExecutionContext.OperationId},
                    {"FlowName", actorExecutionContext.FlowName},
                    {"ApplicationName", actorExecutionContext.ApplicationName},
                    {"Resendable", actorExecutionContext.Resendable},
                    {"SourceSystem", actorExecutionContext.SourceSystem},
                    {"Entity", actorExecutionContext.Entity},
                    {"EntityId", actorExecutionContext.EntityId}
                };
            return dict;
        }
        public static void LogPayload(ILogger Logger, object payload, string actionName, Dictionary<string, object> dictionary, Exception exception = null, string reminderName = null)
        {

            if (exception != null)
            {
                Logger.Log(LogLevel.Error, new EventId(9999),
                    dictionary
                    , exception,
                    (s, ex) =>
                        $"[OnFailedAsync] {s["Actor"]} failed to process message by {s["ActionName"]}. Message: {ex.Message}");
            }
            else
            {
                Logger.Log(LogLevel.Information, new EventId(9999),
                    dictionary
                    , exception,
                    (s, ex) =>
                        $"{s["Actor"]} invokes Internal Process with Action Name {s["ActionName"]} and reminder name: {s["ReminderName"]}");
            }
        }
    }
}
