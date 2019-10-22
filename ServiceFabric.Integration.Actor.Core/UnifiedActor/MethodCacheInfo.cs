using System;
using System.Collections.Generic;

namespace Integration.Common.Actor.UnifiedActor
{
    public class MethodCacheInfo
    {
        public string MethodName { get; set; }
        public string ReturnType {get;set; }
        public string ParameterTypes {get;set;}

        public MethodCacheInfo(string methodName, string returnType, List<string> parameterTypes)
        {
            MethodName = methodName;
            ReturnType = returnType;
            ParameterTypes = String.Join("||", parameterTypes);
        }
    }
}
