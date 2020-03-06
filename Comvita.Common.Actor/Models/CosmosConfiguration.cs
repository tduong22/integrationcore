using Microsoft.Azure.Documents;
using Microsoft.Azure.Documents.Client;

namespace Comvita.Common.Actor.Models
{
    public class CosmosConfiguration
    {
        public RequestOptions RequestOptions { get; set; }
        public IndexingPolicy IndexingPolicy { get; set; }

        public string Endpoint { get; set; }
        public string AuthKey { get; set; }
        public ConnectionPolicy ConnectionPolicy { get; set; }

        public string DatabaseId { get; set; }
        public string CollectionId { get; set; }
        public string PartitionKey { get; set; }
    }
}
