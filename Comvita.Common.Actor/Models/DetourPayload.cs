using System;
using System.Runtime.Serialization;
using Comvita.Common.Actor.Constants;
using Newtonsoft.Json.Linq;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class DetourPayload
    {
        public DetourPayload(string detourKey, string defaultNextActorUri, string nextActorActionName, object nextActorPayload, string nextActorId = null)
        {
            if (string.IsNullOrEmpty(detourKey))
            {
                throw new ArgumentNullException($"Detour payload construct fail: {nameof(DetourKey)} was null");
            }

            if (string.IsNullOrEmpty(defaultNextActorUri))
            {
                throw new ArgumentNullException($"Detour payload construct fail: {nameof(DefaultNextActorUri)} was null");
            }

            if (string.IsNullOrEmpty(nextActorId))
            {
                nextActorId = Guid.NewGuid().ToString();
            }

            NextActorId = nextActorId;
            DetourKey = detourKey;
            DefaultNextActorUri = defaultNextActorUri;

            var jo = JObject.FromObject(nextActorPayload);
            jo.Add(DetourConstants.DETOUR_EXTRA_PROPERTY, DetourKey);
            NextActorPayload = jo.ToString();

            NextActorActionName = nextActorActionName;
        }

        public DetourPayload()
        {
        }

        [DataMember]
        public string DetourKey { get; set; }

        [DataMember]
        public string DefaultNextActorUri { get; set; }

        [DataMember]
        public string NextActorId { get; set; }

        [DataMember]
        public string NextActorPayload { get; set; }

        [DataMember]
        public string NextActorActionName { get; set; }
    }
}
