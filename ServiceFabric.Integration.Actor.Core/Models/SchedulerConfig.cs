using System.Runtime.Serialization;

namespace Integration.Common.Actor.Models
{
    [DataContract]
    public class SchedulerConfig
    {
        [DataMember]
        public string SchedulerName { get; set; }

        [DataMember]
        public int PeriodTimeInSeconds { get; set; }

        [DataMember]
        public string CronExpression { get; set; }
    }
}
