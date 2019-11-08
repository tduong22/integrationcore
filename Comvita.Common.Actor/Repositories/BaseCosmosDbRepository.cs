using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;
using Microsoft.Azure.Documents.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Comvita.Common.Repos.Cosmos
{
    public class BaseCosmosDbRepository<T> : IRepository<T, string> where T : class
    {
        protected readonly DocumentClient _client;
        protected readonly DatabaseConfiguration _databaseConfiguration;

        public BaseCosmosDbRepository(DatabaseConfiguration databaseConfiguration)
        {
            _databaseConfiguration = databaseConfiguration ?? throw new ArgumentNullException(nameof(DatabaseConfiguration));

            var jsonSerializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };

            _client = new DocumentClient(new Uri(_databaseConfiguration.Endpoint), _databaseConfiguration.AuthKey,
                jsonSerializerSettings,
                _databaseConfiguration.ConnectionPolicy);
        }

        public virtual async Task InitializeAsync(string collectionId = null, string partitionKey = null)
        {
            await _client.OpenAsync();
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;
            partitionKey = partitionKey ?? _databaseConfiguration.PartitionKey;

            // Remove this line bc Database always create by manual
            //await CreateDatabaseIfNotExistsAsync(collectionId, partitionKey);

            await CreateCollectionIfNotExistsAsync(collectionId, partitionKey);
        }

        public virtual async Task CreateDatabaseIfNotExistsAsync(string collectionId = null, string partitionKey = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;
            partitionKey = partitionKey ?? _databaseConfiguration.PartitionKey;

            try
            {
                await _client.ReadDatabaseAsync(UriFactory.CreateDatabaseUri(_databaseConfiguration.DatabaseId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await _client.CreateDatabaseIfNotExistsAsync(new Database { Id = _databaseConfiguration.DatabaseId });
                    await CreateDocumentCollection(collectionId, _databaseConfiguration, partitionKey);
                }
            }
        }

        public virtual async Task CreateDocumentCollection(string collectionId,
            DatabaseConfiguration configuration, string partitionKey)
        {
            DocumentCollection documentCollection = new DocumentCollection
            {
                IndexingPolicy = configuration.IndexingPolicy,
                Id = collectionId
            };

            if (!string.IsNullOrEmpty(partitionKey))
            {
                documentCollection.PartitionKey = new PartitionKeyDefinition()
                { Paths = new Collection<string>() { "/" + partitionKey } };
            }

            await _client.CreateDocumentCollectionIfNotExistsAsync(UriFactory.CreateDatabaseUri(_databaseConfiguration.DatabaseId),
                documentCollection);
        }

        public virtual async Task CreateCollectionIfNotExistsAsync(string collectionId = null, string partitionKey = null,
            DatabaseConfiguration configuration = null)
        {
            try
            {
                // set default configuration
                configuration = configuration ?? _databaseConfiguration;
                collectionId = collectionId ?? _databaseConfiguration.CollectionId;
                partitionKey = partitionKey ?? _databaseConfiguration.PartitionKey;

                await _client.ReadDocumentCollectionAsync(
                    UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId));
            }
            catch (DocumentClientException e)
            {
                if (e.StatusCode == System.Net.HttpStatusCode.NotFound)
                {
                    await CreateDocumentCollection(collectionId, configuration, partitionKey);
                }
            }
        }

        public virtual async Task<T> Insert(T dataObject, string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;
            bool disableAutomaticIdGeneration = false;
            var idValue = GetIdentityValue(dataObject);

            if (idValue != null)
            {
                disableAutomaticIdGeneration = true;
            }

            return (T)(dynamic)(await _client.CreateDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId), dataObject,
                null, disableAutomaticIdGeneration)).Resource;
        }

        public virtual async Task<T> Upsert(T dataObject, string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;
            bool disableAutomaticIdGeneration = false;
            var idValue = GetIdentityValue(dataObject);

            if (idValue != null)
            {
                disableAutomaticIdGeneration = true;
            }

            return (T)(dynamic)(await _client.UpsertDocumentAsync(
                UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId), dataObject,
                null, disableAutomaticIdGeneration)).Resource;
        }

        public virtual async Task<T> Update(T dataObject, string id, string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;

            return (T)(dynamic)(await _client.ReplaceDocumentAsync(
                UriFactory.CreateDocumentUri(_databaseConfiguration.DatabaseId, collectionId, id),
                dataObject)).Resource;

        }

        public virtual async Task<T> GetById(string id, string partitionKey = null, string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;

            var documentUri = UriFactory.CreateDocumentUri(_databaseConfiguration.DatabaseId, collectionId, id);
            if (!string.IsNullOrEmpty(partitionKey))
            {
                var requestOpts = new RequestOptions
                {
                    PartitionKey = new PartitionKey(partitionKey)
                };
                var document =
                    await _client.ReadDocumentAsync<T>(documentUri, requestOpts);

                return document;
            }

            if (HasIdentityProperty())
            {
                Uri collectionLink = UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId);

                var findDocumentQuery = _client.CreateDocumentQuery<T>(collectionLink,
                        $"SELECT * FROM c WHERE c.id = '{id}'",
                        new FeedOptions() { EnableCrossPartitionQuery = true, MaxItemCount = -1 })
                    .AsDocumentQuery();
                if (findDocumentQuery.HasMoreResults)
                {
                    var results = await findDocumentQuery.ExecuteNextAsync();
                    return results.First();
                }
            }
            return null;
        }


        public virtual async Task<IEnumerable<T>> GetAllAsync(string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;
            var query = GetQueryable(collectionId).AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;

        }


        public virtual async Task<bool> Delete(string id, string partitionKey = null, string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;

            var requestOpts = new RequestOptions();

            if (string.IsNullOrEmpty(partitionKey))
            {
                var item = await GetById(id);
                var partitionValue = GetPartitionValue(item);
                requestOpts.PartitionKey = string.IsNullOrEmpty(partitionValue)
                    ? new PartitionKey(Undefined.Value)
                    : new PartitionKey(partitionValue);
            }
            else
            {
                requestOpts.PartitionKey = new PartitionKey(partitionKey);
            }

            await _client.DeleteDocumentAsync(
                UriFactory.CreateDocumentUri(_databaseConfiguration.DatabaseId, collectionId, id),
                requestOpts);

            return true;

        }

        public virtual IQueryable<T> GetQueryable(string collectionId = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;

            return _client.CreateDocumentQuery<T>(
                UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId),
                new FeedOptions { MaxItemCount = -1, EnableCrossPartitionQuery = true });
        }

        private string GetPartitionValue(T obj)
        {
            var partitionProperty = obj.GetType().GetProperties().FirstOrDefault(p =>
                p.Name.Equals(_databaseConfiguration.PartitionKey, StringComparison.OrdinalIgnoreCase));
            if (partitionProperty != null)
            {
                var partitionValue = partitionProperty.GetValue(obj);
                if (partitionValue == null)
                {
                    throw new NullReferenceException("Partition value has null");
                }

                return partitionValue.ToString();
            }

            return null;
        }

        private bool HasIdentityProperty()
        {
            return GetIdentityProperty() != null;
        }

        private string GetIdentityValue(T obj)
        {
            var idProperty = GetIdentityProperty();
            if (idProperty != null)
            {
                var idValue = idProperty.GetValue(obj);
                return idValue?.ToString();
            }
            return null;
        }

        private PropertyInfo GetIdentityProperty()
        {
            var idProperty = typeof(T).GetProperties().FirstOrDefault(p => p.Name.Equals("id", StringComparison.OrdinalIgnoreCase));
            if (idProperty != null)
            {
                return idProperty;
            }
            foreach (var property in typeof(T).GetProperties())
            {
                var attrType = typeof(JsonPropertyAttribute);
                if (property.GetCustomAttributes(attrType, false).Any())
                {
                    var jsonPropertyAttribute = (JsonPropertyAttribute)property.GetCustomAttributes(attrType, false).First();

                    if (jsonPropertyAttribute.PropertyName.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        return property;
                    }
                }
            }
            return null;
        }

        public async Task<IEnumerable<T>> SearchFor(Expression<Func<T, bool>> predicate, string collectionId = null, string partitionKey = null)
        {
            collectionId = collectionId ?? _databaseConfiguration.CollectionId;

            IDocumentQuery<T> query = _client.CreateDocumentQuery<T>(UriFactory.CreateDocumentCollectionUri(_databaseConfiguration.DatabaseId, collectionId),
                                                                     new FeedOptions
                                                                     {
                                                                         MaxItemCount = -1,
                                                                         EnableCrossPartitionQuery = (partitionKey == null),
                                                                         PartitionKey = (partitionKey == null) ?
                                                                         PartitionKey.None : new PartitionKey(partitionKey)
                                                                     })
                .Where(predicate)
                .AsDocumentQuery();

            List<T> results = new List<T>();
            while (query.HasMoreResults)
            {
                results.AddRange(await query.ExecuteNextAsync<T>());
            }

            return results;

        }
    }
}
