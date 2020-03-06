using Integration.Common.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace Integration.Common.Flow
{
    public interface IStep
    {

    }
    public interface IFlow<TStep> where TStep : IStep
    {
        IFlow<TStep> Add(TStep step);
        IFlow<TStep> Remove(TStep step);
        TStep GetFirstStep();
        bool ValidateFlow();
        IFlow<TStep> Build();
        TStep GetNextStep(TStep currentStep);
        TStep GetNextStep(StepId currentStepId); //StepId-TStep relationship

        //TStep GetCurrentStep(ActorIdentity requesterIdentity); GetCurrentStep implementation is too concreted?? => TODO: add later
    }

    public enum FlowStatus
    {
        NotStarted = 0,
        Started = 1,
        InProgress = 2,
        Errored = 3,
        Skipped = 4,
        Completed = 5,
        Success = 6
    };

    [JsonObject(NamingStrategyType = typeof(CamelCaseNamingStrategy))]
    [DataContract]
    [KnownType(typeof(Flow))]
    [KnownType(typeof(LinkedList<Step>))]
    public class Flow : IFlow<Step>
    {
        [DataMember]
        public string FlowName = "UN_NAMED";


        [DataMember]
        public string StartedPayload { get; set; }

        [DataMember]
        public string Id { get => FlowId; set => FlowId = value; }

        [DataMember]
        public string FlowId { get; set; }

        [DataMember]
        public LinkedList<Step> LinkedSteps { get; set; }

        [DataMember]
        public FlowStatus FlowStatus { get; set; }

        [DataMember]
        public List<FlowVariable> FlowVariables { get; set; }

        public Flow()
        {
            FlowId = Guid.NewGuid().ToString();
            LinkedSteps = new LinkedList<Step>();
            FlowStatus = FlowStatus.NotStarted;
            FlowVariables = new List<FlowVariable>();
        }

        public Flow(string flowId, string flowName) : this()
        {
            FlowId = flowId;
            FlowName = flowName;
        }

        public IFlow<Step> Add(Step step)
        {
            if (LinkedSteps.Count == 0)
                LinkedSteps.AddFirst(step);
            else
            {
                var lastNode = LinkedSteps.Last;
                LinkedSteps.AddAfter(lastNode, step);
                step.Previous = lastNode.Value;
                lastNode.Value.Next = step;

            }
            step.FlowId = FlowId;
            return this;
        }

        public IFlow<Step> Remove(Step step)
        {
            step.FlowId = null;
            LinkedSteps.Remove(step);
            return this;
        }

        public bool ValidateFlow()
        {
            return true;
        }

        public Step GetFirstStep()
        {
            return LinkedSteps.First(x => !x.IsSpecialStep());
        }

        public Step GetNextStep(Step currentStep)
        {
            if (LinkedSteps.Contains(currentStep))
            {
                var currentStepInFlow = LinkedSteps.Find(currentStep);
                var nextStepInFlow = currentStepInFlow.Next;
                if (nextStepInFlow == null)
                {
                    return Step.EndStep;
                }
                else
                    return nextStepInFlow.Value;
            }
            return Step.NoStep;
        }
        public Step GetNextStep(StepId currentStepId)
        {
            var currentStep = new Step(currentStepId);
            if (LinkedSteps.Contains(currentStep))
            {
                var currentStepInFlow = LinkedSteps.Find(currentStep);
                var nextStepInFlow = currentStepInFlow.Next;
                if (nextStepInFlow == null)
                {
                    return Step.EndStep;
                }
                else
                    return nextStepInFlow.Value;
            }
            return Step.NoStep;
        }

        public Step GetCurrentStep(ActorIdentityWithActionName actorIdentityWithActionName, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false)
        {
            return GetCurrentStep(actorIdentityWithActionName.ActorServiceUri, actorIdentityWithActionName.ActorId, actorIdentityWithActionName.ActionName, needToMatchActionName, occurences, needToMatchSpecificActorId);
        }

        public Step GetCurrentStep(ActorIdentity actorIdentity, bool needToMatchActionName = false, int occurences = 0, bool needToMatchSpecificActorId = false)
        {
            return GetCurrentStep(actorIdentity.ActorServiceUri, actorIdentity.ActorId, null, needToMatchActionName, occurences, needToMatchSpecificActorId);
        }

        public Step GetCurrentStep(string currentActorServiceUri, string currentActorId, string requestedActionName, bool needToMatchActionName, int occurences, bool needToMatchSpecificActorId = false)
        {
            var actorServiceUri = currentActorServiceUri;
            var actorId = currentActorId;
            var actionName = requestedActionName;
            Func<Step, bool> predicate = (x) =>
            {
                var isWithActionName = needToMatchActionName ? x.Orders.Name == actionName : !needToMatchActionName;
                var isSpecifiedActorId = string.IsNullOrEmpty(actorId) || !needToMatchSpecificActorId ? true : x.Orders.GetFirstOrder().ActorId == actorId;
                var isSpecial = x.IsSpecialStep();
                return !isSpecial && isSpecifiedActorId && isWithActionName && x.GetOrchestrationOrder().ActorServiceUri == actorServiceUri;
            };
            var steps = LinkedSteps.Where(predicate).ToList();
            try
            {
                return steps.Count == 0 ? Step.NoStep : steps[occurences];
            }
            catch (IndexOutOfRangeException ex)
            {
                throw new IndexOutOfRangeException($"The occurences {occurences} is invalid. Only provide occurences when there is multiple same steps in your flow. Number of matching step found is {steps.Count}", ex);
            }
        }

        public Step GetCurrentStep(ActorServiceIdentityWithActionName actorserviceIdentityWithActionName, bool needToMatchActionName = false, int occurences = 0)
        {
            return GetCurrentStep(actorserviceIdentityWithActionName.ActorServiceUri, actorserviceIdentityWithActionName.ActorId, actorserviceIdentityWithActionName.ActionName, needToMatchActionName, occurences);
        }

        public Step GetCurrentStep(StepId stepId)
        {
            var currentStep = LinkedSteps.FirstOrDefault(x => x.Id == stepId);
            return currentStep ?? Step.NoStep;
        }

        public IFlow<Step> Build()
        {
            if (ValidateFlow())
            {
                foreach (var step in LinkedSteps)
                {
                    if (step.IsValid())
                    {
                        //
                    }
                }
            }
            return this;
        }

        public bool Complete()
        {
            FlowStatus = FlowStatus.Completed;
            return true;
        }

        public bool Error()
        {
            FlowStatus = FlowStatus.Errored;
            return true;
        }

        public bool Start()
        {
            FlowStatus = FlowStatus.Started;
            return true;
        }

        public void AddFlowVariables(FlowVariable flowVariable)
        {
            var exist = FlowVariables.Find(c => c.Name == flowVariable.Name);
            if (exist == null)
                FlowVariables.Add(flowVariable);
            else
                exist = flowVariable;
        }

        public void SetStartedPayload(string payload)
        {
            StartedPayload = payload;
        }

        public Step GetCurrentStep(ActorIdentity currentActorServiceWithActorIdNotSpecified)
        {
            throw new NotImplementedException();
        }
    }

    public class StartStep : Step
    {

    }
    public class NoStep : Step
    {

    }
    public class EndStep : Step
    {

    }
    public enum StepType
    {
        Normal = 0,
        AsynchronousSending = 1,
        AsynchronousReceiving = 2
    }
    public class StepId : IEquatable<StepId>
    {
        public StepId()
        {

        }
        public StepId(string uniqueId)
        {
            UniqueId = uniqueId;
        }

        public string UniqueId { get; set; }

        public bool Equals(StepId other)
        {
            return UniqueId == other.UniqueId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as StepId);
        }

        public override int GetHashCode()
        {
            return UniqueId.GetHashCode();
        }


    }
}

