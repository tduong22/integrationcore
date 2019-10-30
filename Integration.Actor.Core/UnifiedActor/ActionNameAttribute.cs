using System;

namespace Integration.Common.Actor.UnifiedActor
{
    [System.ComponentModel.Composition.MetadataAttribute]
    public class ActionNameAttribute : Attribute
    {
        public string ActionName { get; set; }

        public ActionNameAttribute(string actionName)
        {
            this.ActionName = actionName;
        }
    }

    [System.ComponentModel.Composition.MetadataAttribute]
    public class FlowKeyAttribute : Attribute
    {
        public string FlowKey { get; set; }

        public FlowKeyAttribute(string flowKey)
        {
            this.FlowKey = flowKey;
        }
    }
}
