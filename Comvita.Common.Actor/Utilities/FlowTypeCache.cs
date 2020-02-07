using Integration.Common.Actor.UnifiedActor;
using System;
using System.Reflection;

namespace Comvita.Common.Actor.Utilities
{
    public static class FlowCacheFor<T>
    {
        static FlowCacheFor()
        {
            // grab the data
            Value = ExtractFlowAttribute(typeof(T));
        }

        public static readonly string Value;

        private static string ExtractFlowAttribute(Type type)
        {
            return type.GetCustomAttribute<ActionNameAttribute>().ActionName;
        }
    }
}
