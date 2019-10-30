using System;

namespace Integration.Common.Flow
{
    public class Step<TInput> : Step
    {
        public TInput Payload { get; set; }

        public Func<TInput, string> ActorIdRetrieveFromPayload { get; set; }

        public Step()
        {

        }

        public Step(string actorServiceUri, string actorId = null, string actionName = null, StepType stepType = StepType.Normal) : base(actorServiceUri, actorId, actionName, stepType)
        {
            TypeOfPayload = typeof(TInput);
        }
    }
}

