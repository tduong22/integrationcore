using Integration.Common.Interface;
using System;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Integration.Common.Flow
{
    public class Step<TInput, TOutput> : Step<TInput>
    {
        public Step(string actorServiceUri, string actorId, string actionName = null, StepType stepType = StepType.Normal) : base(actorServiceUri: actorServiceUri, actorId, actionName, stepType)
        {
            TypeOfOutputPayload = typeof(TOutput);
        }

        public override bool IsValid()
        {
            return base.IsValid();
        }
    }

    public class Step<TInput, TOutput, TIActionInterface> : Step<TInput, TOutput> where TIActionInterface : IRemotableAction
    {
        public Step(string actorServiceUri, string actorId, string methodName, StepType stepType = StepType.Normal) : base(actorServiceUri: actorServiceUri, actorId, typeof(TIActionInterface).Name, stepType)
        {
            MethodName = methodName;
        }

        public Step(string actorServiceUri, string actorId, Expression<Func<TIActionInterface, TOutput, Task>> expression, StepType stepType = StepType.Normal) : this(actorServiceUri: actorServiceUri, actorId, ((MethodCallExpression)expression.Body).Method.Name, stepType)
        {
        }
    }
}

