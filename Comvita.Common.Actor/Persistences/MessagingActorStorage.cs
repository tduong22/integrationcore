using Integration.Common.Actor.Helpers;
using Integration.Common.Actor.Interface;
using Integration.Common.Interface;
using Integration.Common.Model;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Client;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Persistences
{
    public class MessagingActorStorage : IKeyValueStorage<string>
    {
        protected const string STORAGE_KEY_PREFIX = "STORAGE";
        protected Uri StorageServiceUri;

        protected IBinaryMessageSerializer BinaryMessageSerializer;

        public MessagingActorStorage(IBinaryMessageSerializer binaryMessageSerializer, StorageServiceInfo storageServiceInfo)
        {
            BinaryMessageSerializer = binaryMessageSerializer;
            StorageServiceUri = storageServiceInfo.ServiceUri;
        }

        public async Task<object> RetrieveMessageAsync(string key, Type typeOfPayload, CancellationToken cancellationToken)
        {
            var proxy = ActorProxy.Create<IStorageActor>(new ActorId($"{NameCompositionResolver.ExtractFlowInstanceIdFromFlowVariableKey(key)}"), StorageServiceUri);
            var rawData = await proxy.RetrieveMessageAsync(new ActorRequestContext(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()), key, false, cancellationToken);

            var data = rawData == null ? null : BinaryMessageSerializer.DeserializePayload(rawData, typeOfPayload);
            return data;
        }

        public async Task<TPayload> RetrieveMessageAsync<TPayload>(string key, CancellationToken cancellationToken)
        {
            var dataObject = await RetrieveMessageAsync(key, typeof(TPayload), cancellationToken);
           return (TPayload) dataObject;
        }

        public Task<string> StoreMessageAsync(string key, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            var serializedData = BinaryMessageSerializer.SerializePayload(payload, typeOfPayload);
            var proxy = ActorProxy.Create<IStorageActor>(new ActorId($"{NameCompositionResolver.ExtractFlowInstanceIdFromFlowVariableKey(key)}"), StorageServiceUri);
            return proxy.SaveMessageAsync(new ActorRequestContext(Guid.NewGuid().ToString(), null, Guid.NewGuid().ToString()), key, serializedData, cancellationToken);
        }

        public Task<string> StoreMessageAsync<TPayload>(string key, TPayload payload, CancellationToken cancellationToken)
        {
            return StoreMessageAsync(key, payload, typeof(TPayload), cancellationToken);
        }

        public async Task ClearMessageAsync(string key, CancellationToken cancellationToken)
        {
            var actorId = new ActorId($"{STORAGE_KEY_PREFIX}_{key}");
            DisposeActor(actorId, StorageServiceUri, cancellationToken);
        }

            /// <summary>
        /// Schedule a task on the thread pool to delete the actor with a specific Id. Override if needed
        /// </summary>
        /// <param name="cancellationToken"></param>
        protected virtual void DisposeActor(ActorId actorId, Uri actorServiceUri, CancellationToken cancellationToken)
        {
                Task.Run(async () =>
                {
                    var serviceProxy = ActorServiceProxy.Create(actorServiceUri, actorId);
                    await serviceProxy.DeleteActorAsync(actorId, cancellationToken);
                }, cancellationToken);
        }
    }
}
