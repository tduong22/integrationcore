using Integration.Common.Exceptions;
using Integration.Common.Model;
using Newtonsoft.Json;
using System;
using System.Runtime.Serialization;

namespace Integration.Common.Flow
{
    public enum StepStatus
    {
        NotStarted = 0,
        Started = 1,
        InProgress = 2,
        Errored = 3,
        Skipped = 4,
        Completed = 5
    };

    [DataContract(IsReference = true)]
    public class Step : IEquatable<Step>, IStep
    {
        public const string END_STEP_GUID = "968ecd24-47ed-4b4f-b14d-ac1c9b64bdfb";
        public const string NO_STEP_GUID = "9643134e-1e04-4e25-9bac-97621a372d0a";

        [DataMember]
        /// <summary>
        /// Reference to the flow id
        /// </summary>
        public string FlowId { get; set; }

        [IgnoreDataMember]
        /// <summary>
        /// Friendly name of the step
        /// </summary>
        public string StepName => GetOrchestrationOrderCollection()?.Name;

        [IgnoreDataMember]
        public Type TypeOfPayload { get; set; }

        [IgnoreDataMember]
        public Type TypeOfOutputPayload { get; set; }

        [DataMember]
        public StepId Id { get; set; }

        [DataMember]
        public StepType StepType { get; set; }

        [DataMember]
        public string JsonData { get; set; }

        [IgnoreDataMember]
        public Func<Step, string> ActorIdRetrieveFromPreviousStep { get; set; }

        [DataMember]
        public Step Previous { get; set; }

        [DataMember]
        public Step Next { get; set; }

        [DataMember]
        public bool IsPauseAble { get; set; }

        [DataMember]
        public OrchestrationOrderCollection Orders { get; set; }

        [DataMember]
        public StepStatus StepStatus { get; set; }

        [DataMember]
        public string MethodName { get; set; }

        public bool IsSpecialStep()
        {
            return (this) == NoStep || (this) == EndStep;
        }

        public Step()
        {
            Id = new StepId(Guid.NewGuid().ToString());
            Orders = OrchestrationOrderCollection.NoOrder();
            StepType = StepType.Normal;
        }

        public Step(StepType stepType = StepType.Normal) : this()
        {
            StepType = StepType.Normal;
        }

        public Step(string actorServiceUri, string actorId = null, string actionName = null, StepType stepType = StepType.Normal) : this()
        {
            Orders.CreateSingleOrder(actionName, new OrchestrationOrder(actorServiceUri, actorId));
            StepType = stepType;
        }

        public Step(StepId id, StepType stepType = StepType.Normal) : this()
        {
            Id = id;
            StepType = stepType;
        }

        public OrchestrationOrder GetOrchestrationOrder()
        {
            return Orders.GetFirstOrder();
        }

        public bool Equals(Step other)
        {
            if (ReferenceEquals(null, other)) return false;
            return Id.Equals(other.Id);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals(obj as Step);
        }

        public override int GetHashCode()
        {
            return Id.GetHashCode();
        }
        public static bool operator ==(Step step1, Step step2)
        {
            if (ReferenceEquals(step1, null))
            {
                if (ReferenceEquals(step2, null))
                {
                    // null == null = true.
                    return true;
                }

                // Only the left side is null.
                return false;
            }
            // Equals handles case of null on right side.
            return step1.Equals(step2);
        }

        public static bool operator !=(Step step1, Step step2)
        {
            if (ReferenceEquals(step1, null))
            {
                if (ReferenceEquals(step2, null))
                {
                    // null == null = true.
                    return false;
                }
                // Only the left side is null.
                return true;
            }

            if (ReferenceEquals(step2, null))
            {
                //only right side is null
                return true;
            }
            // Only the left side is null.
            return !(step1 == step2);
        }
        public static NoStep NoStep { get => new NoStep() { Id = new StepId(NO_STEP_GUID), Orders = OrchestrationOrderCollection.NoOrder() }; }

        public static EndStep EndStep { get => new EndStep() { Id = new StepId(END_STEP_GUID), Orders = OrchestrationOrderCollection.NoOrder() }; }

        public ExecutableOrchestrationOrder ToExecutableOrder()
        {
            return Orders.GetFirstExecutableOrder();
        }
        public virtual bool IsValid()
        {
            var isValidUrl = IsValidFabricUrl();
            return isValidUrl;
        }

        public OrchestrationOrderCollection GetOrchestrationOrderCollection()
        {
            return Orders;
        }

        public string GetPredefinedActionName()
        {
            return Orders?.Name;
        }

        public bool IsValidFabricUrl()
        {
            if (!IsSpecialStep()) // only check for normal step, ignore special step like end step & no step
            {
                var isValid = GetOrchestrationOrder()?.ActorServiceUri?.Contains($"fabric:/") == true;

                if (!isValid)
                    throw new StepValidationException(StepName, $"ServiceUri is wrong. Current step uri is {GetOrchestrationOrder()?.ActorServiceUri}");
            }
            return true;
        }

        public bool Complete()
        {
            StepStatus = StepStatus.Completed;
            return true;
        }

        public bool Start()
        {
            StepStatus = StepStatus.Started;
            return true;
        }

        public bool Error()
        {
            StepStatus = StepStatus.Errored;
            return true;
        }


        public void SetJsonPayload(object data)
        {
            JsonData = JsonConvert.SerializeObject(data);
        }
    }
}

