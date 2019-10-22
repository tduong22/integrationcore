namespace Integration.Common.Flow
{
    public enum FlowLogicStep
    {
        Positive,
        Negative,
        Unknown = 9999
    }

    /// <summary>
    /// Logical step to jump to other flow
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class LogicStep : Step
    {
        protected Flow PositiveFlow;
        protected Flow NegativeFlow;

        public LogicStep(Flow positiveFlow, Flow negativeFlow, string actorServiceUri, string actorId = null, string actionName = null, StepType stepType = StepType.Normal)
            : base(actorServiceUri, actorId, actionName, stepType)
        {

        }

        public Step GetNextStep(FlowLogicStep flowLogicStep)
        {
            return flowLogicStep == FlowLogicStep.Positive ? PositiveFlow.GetFirstStep() : NegativeFlow.GetFirstStep();
        }

        public Flow GetPositiveFlow() => PositiveFlow;

        public Flow GetNegativeFlow() => NegativeFlow;
    }
}
