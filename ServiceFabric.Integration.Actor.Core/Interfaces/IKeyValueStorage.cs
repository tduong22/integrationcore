using System;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Interface
{
    public interface IKeyValueStorage<TKey>
    {
        Task<TKey> StoreMessageAsync(TKey key, object payload, Type typeOfPayload, CancellationToken cancellationToken);
        Task<TKey> StoreMessageAsync<TPayload>(TKey key, TPayload payload, CancellationToken cancellationToken);
        Task<object> RetrieveMessageAsync(TKey key, Type typeOfPayload, CancellationToken cancellationToken);
        Task<TPayload> RetrieveMessageAsync<TPayload>(TKey key, CancellationToken cancellationToken);
        Task ClearMessageAsync(string key, CancellationToken cancellationToken);
    }
}
