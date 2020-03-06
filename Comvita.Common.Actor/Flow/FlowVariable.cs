using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Flow
{
    [DataContract]
    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    public class FlowVariable
    {
        [DataMember]
        public string Name {get;set; }
        
        [DataMember]
        public string Data {get;set; }
    }
}
