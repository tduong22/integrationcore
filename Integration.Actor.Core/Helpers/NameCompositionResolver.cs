using System;
using Integration.Common.Actor.Persistences;

namespace Integration.Common.Actor.Helpers
{
    public static class NameCompositionResolver
    {
        public const string INVALID_ACTION_NAME = "INVALID_ACTION_NAME";
        public static string GenerateReminderName(string actionName, string requestContextId)
        {
            return actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }

        public static string ExtractRequestContextIdFromReminderName(string reminderName)
        {
            return reminderName.Split(ActorRequestPersistence.STATE_NAME_DELIMITER)[1];
        }

        public static string ExtractActionNameFromReminderName(string reminderName)
        {
            try
            {
                return reminderName.Split(ActorRequestPersistence.STATE_NAME_DELIMITER)[0];
            }
            catch (Exception ex)
            {
                return INVALID_ACTION_NAME;
            }
        }

        public static string GenerateRequestCanncellationTokenStateName(string actionName, string requestContextId)
        {
            return ActorRequestPersistence.CANCELLATION_TOKEN_STATE_NAME +
                   ActorRequestPersistence.STATE_NAME_DELIMITER + actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }

        public static string GenerateRequestPayloadStateName(string actionName, string requestContextId)
        {
            return ActorRequestPersistence.PAYLOAD_STATE_NAME +
                   ActorRequestPersistence.STATE_NAME_DELIMITER + actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }

        public static string GenerateRequestPayloadTypeStateName(string actionName, string requestContextId)
        {
            return ActorRequestPersistence.PAYLOAD_TYPE_STATE_NAME +
                   ActorRequestPersistence.STATE_NAME_DELIMITER + actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }


        public static string GenerateRequestContextStateName(string actionName, string requestContextId)
        {
            return ActorRequestPersistence.REQUEST_CONTEXT_STATE_NAME +
                   ActorRequestPersistence.STATE_NAME_DELIMITER + actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }

        public static string GenerateRequestStateStateName(string actionName, string requestContextId)
        {
            return ActorRequestPersistence.REQUEST_STATES_STATE_NAME +
                   ActorRequestPersistence.STATE_NAME_DELIMITER + actionName + ActorRequestPersistence.STATE_NAME_DELIMITER + requestContextId;
        }

        public static string GenerateFlowVariableStorageKey(string key, string flowInstanceId)
        {
            return $"{flowInstanceId}_{key}";
        }

        public static string ExtractFlowInstanceIdFromFlowVariableKey(string fullKey)
        {
            return fullKey.Split('_')[0];
        }

        public static bool IsValidRequestPersistenceForNonBlocking(string reminderName)
        {
            bool isValid = true;
            if (!reminderName.Contains(ActorRequestPersistence.STATE_NAME_DELIMITER.ToString())) return false;

            var splits = reminderName.Split(ActorRequestPersistence.STATE_NAME_DELIMITER);
            if (splits.Length < 2)
            {
                return false;
            }
            return isValid;
        }
    }
}
