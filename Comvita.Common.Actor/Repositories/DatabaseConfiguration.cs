using System.Collections.ObjectModel;
using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Comvita.Common.Repos.Cosmos
{
    public class DatabaseConfiguration
    {
        public RequestOptions RequestOptions { get; set; }
        public IndexingPolicy IndexingPolicy { get; set; }

        public string Endpoint { get; set; }
        public string AuthKey { get; set; }
        public ConnectionPolicy ConnectionPolicy { get; set; }

        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
        public string PartitionKey { get; set; }


        public DatabaseConfiguration(string endpoint, string authKey, ConnectionPolicy connectionPolicy, string databaseId, string collectionId, string partitionKey)
        {
            RequestOptions = new RequestOptions { OfferThroughput = 1000 };
            IndexingPolicy = new IndexingPolicy()
            {
                IncludedPaths = new Collection<IncludedPath>() {
                    new IncludedPath{
                        Path = "/*",
                        Indexes = new Collection<Index>(){
                            new RangeIndex(DataType.String) { Precision = -1 },
                            new RangeIndex(DataType.Number) { Precision = -1 },
                            new SpatialIndex(DataType.Point)
                        }
                    }
                }
            };

            Endpoint = endpoint;
            AuthKey = authKey;
            ConnectionPolicy = connectionPolicy;
            CollectionId = collectionId;
            PartitionKey = partitionKey;
            DatabaseId = databaseId;
        }

        public DatabaseConfiguration(RequestOptions requestOptions, IndexingPolicy indexingPolicy)
        {
            RequestOptions = requestOptions;
            IndexingPolicy = indexingPolicy;
        }
    }
}
