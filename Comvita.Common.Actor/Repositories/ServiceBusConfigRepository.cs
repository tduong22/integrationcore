using Comvita.Common.Actor.Models;
using Comvita.Common.Repos.Cosmos;

namespace Comvita.Common.Actor.Repositories
{
    public class ServiceBusConfigRepository : BaseCosmosDbRepository<ServiceBusConfig>
    {
        public ServiceBusConfigRepository(DatabaseConfiguration databaseConfiguration) : base(databaseConfiguration)
        {
        }
    }
}
