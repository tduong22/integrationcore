using Comvita.Common.Repos.Cosmos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Comvita.Common.Actor.Persistences
{
    public class DefaultRepository<T> : IRepository<T,string>
    {
        private const string _DEFAULT_IMPLEMENTATION_IN_USE_MESSAGE = "The default repository is in use because of there are exceptions with registering and resolving the correct instance";
        public DefaultRepository()
        {
            
        }
        public Task InitializeAsync(string collectionId = null, string partitionKey = null)
        {
            throw new NotImplementedException();
        }

        public Task CreateDatabaseIfNotExistsAsync(string collectionId = null, string partitionKey = null)
        {
            throw new NotImplementedException();
        }

        public Task CreateCollectionIfNotExistsAsync(string collectionId = null, string partitionKey = null,
            DatabaseConfiguration configuration = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> Insert(T dataObject, string collectionId = null)
        {
            //throw new NotImplementedException();
            Console.WriteLine(_DEFAULT_IMPLEMENTATION_IN_USE_MESSAGE);
            return Task.FromResult(dataObject);
        }

        public Task<T> Update(T dataObject, string id, string collectionId = null)
        {
            Console.WriteLine(_DEFAULT_IMPLEMENTATION_IN_USE_MESSAGE);
            return Task.FromResult(dataObject);
        }

        public Task<T> Upsert(T dataObject, string collection = null)
        {
            Console.WriteLine(_DEFAULT_IMPLEMENTATION_IN_USE_MESSAGE);
            return Task.FromResult(dataObject);
        }

        public Task<bool> Delete(string id, string partitionKey = null, string collectionId = null)
        {
            throw new NotImplementedException();
        }

        public Task<T> GetById(string id, string partitionKey = null, string collectionId = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> GetAllAsync(string collectionId = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> SearchFor(Expression<Func<T, bool>> predicate, string collectionId = null)
        {
            throw new NotImplementedException();
        }

        public IQueryable<T> GetQueryable(string collectionId = null)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<T>> SearchFor(Expression<Func<T, bool>> predicate, string collectionId = null, string partitionKey = null)
        {
            throw new NotImplementedException();
        }
    }
}
