﻿using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Integration.Common.Actor.UnifiedActor
{
    [KnownType(typeof(List<SerializableMethodArgument>))]
    [KnownType(typeof(List<string>))]
    [DataContract]
    public class SerializableMethodInfo
    {
        [DataMember]
        public string MethodName { get; set; }

        [DataMember]
        public List<SerializableMethodArgument> Arguments { get; set; }

        [DataMember]
        public bool IsGenericMethod { get; set; }

        [DataMember]
        public List<string> GenericAssemblyTypes { get; set; }

        public SerializableMethodInfo()
        {
            Arguments = new List<SerializableMethodArgument>();
        }
    }

    [KnownType(typeof(byte[]))]
    [DataContract]
    public class SerializableMethodArgument
    {
        [DataMember]
        public string ArgumentName { get; set; }

        [DataMember]
        public string ArgumentAssemblyType { get; set; }

        [DataMember]
        public byte[] Value { get; set; }
    }

}
