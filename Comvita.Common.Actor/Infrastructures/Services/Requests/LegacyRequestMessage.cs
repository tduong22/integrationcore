using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Infrastructures.Services.Requests
{
    [DataContract]
    public class LegacyRequestMessage
    {
        [DataMember]
        public string RequestUri { get; set; }

        [DataMember]
        public string Method { get; set; }

        [DataMember]
        public string Content { get; set; }

        [DataMember]
        public Dictionary<string, string> Headers { get; set; }
    }
}
