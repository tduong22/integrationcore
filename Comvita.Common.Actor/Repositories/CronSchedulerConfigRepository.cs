using Comvita.Common.Actor.Models;
using Comvita.Common.Repos.Cosmos;

namespace Comvita.Common.Actor.Repositories
{
    public class CronSchedulerConfigRepository : BaseCosmosDbRepository<CronSchedulerConfig>
    {
        public CronSchedulerConfigRepository(DatabaseConfiguration databaseConfiguration) : base(databaseConfiguration)
        {
        }
    }
}
