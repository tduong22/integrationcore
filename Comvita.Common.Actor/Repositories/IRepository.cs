using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Comvita.Common.Repos.Cosmos
{
    public interface IRepository<T, in TKey>
    {
        Task InitializeAsync(string collectionId = null, string partitionKey = null);
        Task CreateDatabaseIfNotExistsAsync(string collectionId = null, string partitionKey = null);
        Task CreateCollectionIfNotExistsAsync(string collectionId = null, string partitionKey = null, DatabaseConfiguration configuration = null);
        Task<T> Insert(T dataObject, string collectionId = null);
        Task<T> Update(T dataObject, TKey id, string collectionId = null);
        Task<T> Upsert(T dataObject, string collection = null);
        Task<bool> Delete(TKey id, string partitionKey = null, string collectionId = null);
        Task<T> GetById(TKey id, string partitionKey = null, string collectionId = null);
        Task<IEnumerable<T>> GetAllAsync(string collectionId = null);
        Task<IEnumerable<T>> SearchFor(Expression<Func<T, bool>> predicate, string collectionId = null, string partitionKey = null);
        IQueryable<T> GetQueryable(string collectionId = null);
    }
}
