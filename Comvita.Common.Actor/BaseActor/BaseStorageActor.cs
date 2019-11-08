using System;
using System.Threading;
using System.Threading.Tasks;
using Integration.Common.Actor.BaseActor;
using Integration.Common.Actor.Interface;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.Extensions.Logging;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace Comvita.Common.Actor.BaseActor
{
    public abstract class BaseStorageActor : BaseMessagingActor, IStorageActor, IRemindable
    {
        public const string SAVED_ERROR = "SAVED_ERROR";
        public const string TTL_REMINDER_NAME = "TTL_REMINDER_NAME";

        protected IActorReminder ActorReminder;
        protected int MAX_TTL_IN_MINUTE = 60;

        protected BaseStorageActor(ActorService actorService, ActorId actorId, IBinaryMessageSerializer binaryMessageSerializer, Integration.Common.Actor.Interface.IActorClient actorClient, Integration.Common.Interface.IKeyValueStorage<string> storage, ILogger logger) : base(actorService, actorId, binaryMessageSerializer, actorClient, storage, logger)
        {
        }

        public async Task<byte[]> RetrieveMessageAsync(ActorRequestContext actorRequestContext, string key, bool isOptional, CancellationToken cancellationToken)
        {
            try
            {
                //anytime a request to retrieve data, re-schedule reminder to new one
                await RegisterReminderAsync(TTL_REMINDER_NAME, null, TimeSpan.FromDays(MAX_TTL_IN_MINUTE), TimeSpan.FromMilliseconds(-1));
                return await StateManager.GetStateAsync<byte[]>(key);
            }
            catch (System.Exception ex)
            {
                if (isOptional) return null;
                Logger.LogError(ex, $"Failed to retrieve the variable with key {key} from {actorRequestContext?.ManagerId}.");
                throw;
            }
        }

        public async Task<string> SaveMessageAsync(ActorRequestContext actorRequestContext, string key, byte[] payload, CancellationToken cancellationToken)
        {
            try
            {
                // save message => schedule to delete after TTL
                await RegisterReminderAsync(TTL_REMINDER_NAME, null, TimeSpan.FromDays(MAX_TTL_IN_MINUTE), TimeSpan.FromMilliseconds(-1));
                await StateManager.AddOrUpdateStateAsync(key, payload, (k, v) => payload, cancellationToken);
                return key;
            }
            catch (System.Exception ex)
            {
                Logger.LogError(ex, $"Failed to store the variable with key {key}.");
                return SAVED_ERROR;
            }
        }

        public async Task ReceiveReminderAsync(string reminderName, byte[] state, TimeSpan dueTime, TimeSpan period)
        {
            if (reminderName.Equals(TTL_REMINDER_NAME))
            {
                Logger.LogInformation($"Releasing resource for actor id {Id.ToString()}");
                //dispose itself
                DisposeActor(Id, ServiceUri, CancellationToken.None);
            }
        }
    }
}
