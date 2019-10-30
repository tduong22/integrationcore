using Integration.Common.Interface;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceFabric.Integration.Actor.Core.Persistences
{
    public class NullKeyVaultStorage : IKeyValueStorage<string>
    {
        public Task ClearMessageAsync(string key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<object> RetrieveMessageAsync(string key, Type typeOfPayload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<TPayload> RetrieveMessageAsync<TPayload>(string key, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> StoreMessageAsync(string key, object payload, Type typeOfPayload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<string> StoreMessageAsync<TPayload>(string key, TPayload payload, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
