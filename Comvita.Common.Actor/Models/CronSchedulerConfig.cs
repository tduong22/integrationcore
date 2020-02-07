using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class CronSchedulerConfig : BaseSchedulerConfig
    {
        [DataMember]
        public string CronName { get; set; }

        [DataMember]
        public string CronExpression { get; set; }

        [DataMember]
        public bool Enabled {get;set; }

        public CronSchedulerConfig()
        {
            Id = CronName;
            PartitionKey = Type = nameof(CronSchedulerConfig);
        }
    }
}
