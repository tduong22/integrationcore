using Integration.Common.Actor.Helpers;
using Integration.Common.Actor.Interface;
using Integration.Common.Flow;
using Integration.Common.Interface;
using Integration.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace Integration.Common.Actor.UnifiedActor
{
    public static class ActionInvoker
    {
        private static IActorClient _actorClient;
        private static IBinaryMessageSerializer _binaryMessageSerializer;

        public const string INVALID_TARGET_ACTOR_ERROR_MESSAGE = "ActionInvoker MUST be invoked with an actor request context having tartget actor identity via TargetActor property of actorequestcontext or provided by an executable order class";

        static ActionInvoker()
        {
            _actorClient = BaseDependencyResolver.ResolveActorClient();
            _binaryMessageSerializer = BaseDependencyResolver.ResolveBinarySerializer();
        }

        public static Task Invoke<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction
        => ActionInvoker<TIActionInterface>.Invoke(expression, actorRequestContext, cancellationToken);

        public static Task Invoke<TIActionInterface>(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken) where TIActionInterface : IRemotableAction
        => ActionInvoker<TIActionInterface>.Invoke(expression, actorRequestContext, executableOrchestrationOrder, cancellationToken);

        public static Task Invoke(string methodName, ActorRequestContext actorRequestContext, ExecutableOrchestrationOrder executableOrchestrationOrder, params object[] arguments)
        {
            actorRequestContext.TargetActor = new ActorIdentity(executableOrchestrationOrder.ActorId, executableOrchestrationOrder.ActorServiceUri);
            return Invoke(methodName, actorRequestContext, arguments);
        }

        /// <summary>
        /// This method will allow supports for Flow&Step involed in the implementation without a strong type expression of the interface
        /// This method also help when we have methodName resolved by a serialized Step
        /// </summary>
        /// <param name="methodName"></param>
        /// <param name="actorRequestContext"></param>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static async Task Invoke(string methodName, ActorRequestContext actorRequestContext, params object[] arguments)
        {
            if (actorRequestContext?.TargetActor == null)
            {
                throw new ArgumentNullException(INVALID_TARGET_ACTOR_ERROR_MESSAGE);
            }

            var serializableMethodInfo = new SerializableMethodInfo();
            serializableMethodInfo.MethodName = methodName;
            actorRequestContext.MethodName = methodName;

            foreach (var arg in arguments)
            {
                serializableMethodInfo.Arguments.Add(new SerializableMethodArgument()
                {
                    ArgumentAssemblyType = arg.GetType().AssemblyQualifiedName,
                    Value = _binaryMessageSerializer.SerializePayload(arg)
                });
            }
            actorRequestContext.ActionName = actorRequestContext.ActionName;
            await _actorClient.ChainNextActorAsync<SerializableMethodInfo>(actorRequestContext, serializableMethodInfo, actorRequestContext.TargetActor, CancellationToken.None);
        }
    }

    public static class ActionInvoker<TIActionInterface> where TIActionInterface : IRemotableAction
    {
        //TODO: Cache expression parsing for the type of interface & arguments if needed
        //private static ConcurrentDictionary<MethodCacheInfo, List<SerializableMethodArgument>> Cache;

        private static IActorClient _actorClient;
        private static IBinaryMessageSerializer _binaryMessageSerializer;
        static ActionInvoker()
        {
            _actorClient = BaseDependencyResolver.ResolveActorClient();
            _binaryMessageSerializer = BaseDependencyResolver.ResolveBinarySerializer();
        }

        public static Task Invoke(Expression<Func<TIActionInterface, object>> expression, ActorIdentity tartgetActor, CancellationToken cancellationToken) {
            //form a default generated actor request context
            var actorRequestContext = new ActorRequestContext("DefaultGenetatedManagerId", "NoActionNameGeneratedYet", Guid.NewGuid().ToString(), tartgetActor, FlowInstanceId.NewFlowInstanceId);
            return Invoke(expression, actorRequestContext, cancellationToken);
        }

        public async static Task Invoke(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, CancellationToken cancellationToken)
        {
            ValidateRequest(actorRequestContext);

            var serializableMethodInfo = new SerializableMethodInfo();
            var body = expression.Body;
            var methodCallExpression = (MethodCallExpression)body;
            if (methodCallExpression.Method.ReturnType != typeof(Task))
            {
                throw new NotSupportedException($"Method return type that are invoked by ActionInvoker must be Task for asynchronous request. Current is {methodCallExpression.Method.ReturnType} which is not supported at the moment");
            }

            serializableMethodInfo.MethodName = methodCallExpression.Method.Name;
            actorRequestContext.MethodName = serializableMethodInfo.MethodName;

            var methodInfo = methodCallExpression.Method;

            var args = ResolveArgs<TIActionInterface>(expression);

            foreach (var arg in args)
            {
                serializableMethodInfo.Arguments.Add(new SerializableMethodArgument()
                {
                    ArgumentAssemblyType = arg.Key.AssemblyQualifiedName,
                    Value = _binaryMessageSerializer.SerializePayload(arg.Value)
                });
            }

            if (methodInfo.IsGenericMethod)
            {
                serializableMethodInfo.IsGenericMethod = true;
                serializableMethodInfo.GenericAssemblyTypes = new List<string>();
                foreach (var type in methodInfo.GetGenericArguments())
                {
                    serializableMethodInfo.GenericAssemblyTypes.Add(type.AssemblyQualifiedName);
                }
            }
            actorRequestContext.ActionName = typeof(TIActionInterface).Name;
            await _actorClient.ChainNextActorAsync<SerializableMethodInfo>(actorRequestContext, serializableMethodInfo, actorRequestContext.TargetActor, cancellationToken);
        }

        public static Task Invoke(Expression<Func<TIActionInterface, object>> expression, ActorRequestContext actorRequestContext, ExecutableOrchestrationOrder executableOrchestrationOrder, CancellationToken cancellationToken)
        { 
            actorRequestContext.TargetActor = new ActorIdentity(executableOrchestrationOrder.ActorId, executableOrchestrationOrder.ActorServiceUri);
            return Invoke(expression, actorRequestContext, cancellationToken);
        }
        private static void ValidateRequest(ActorRequestContext actorRequestContext)
        {
            if (actorRequestContext?.TargetActor == null)
            {
                throw new ArgumentNullException(ActionInvoker.INVALID_TARGET_ACTOR_ERROR_MESSAGE);
            }

            if (nameof(TIActionInterface) == nameof(IRemotableAction))
                throw new InvalidOperationException($"ActionInvoker MUST NOT be invoked directly on {nameof(IRemotableAction)} due to the interface names will be resolved by DI Containers");
        }

        #region Private parsing & processing method expression
        private static KeyValuePair<Type, object>[] ResolveArgs<T>(Expression<Func<T, object>> expression)
        {
            var body = (System.Linq.Expressions.MethodCallExpression)expression.Body;
            var values = new List<KeyValuePair<Type, object>>();

            foreach (var argument in body.Arguments)
            {
                if (argument is ConstantExpression constantExpression)
                {
                    values.Add(new KeyValuePair<Type, object>(constantExpression.Type, constantExpression.Value));
                }
                else
                {
                    var exp = ResolveMemberExpression(argument);
                    var type = argument.Type;
                    var value = GetValue(exp);

                    values.Add(new KeyValuePair<Type, object>(type, value));
                }
            }

            return values.ToArray();
        }

        public static MemberExpression ResolveMemberExpression(Expression expression)
        {

            if (expression is MemberExpression)
            {
                return (MemberExpression)expression;
            }
            else if (expression is UnaryExpression)
            {
                // if casting is involved, Expression is not x => x.FieldName but x => Convert(x.Fieldname)
                return (MemberExpression)((UnaryExpression)expression).Operand;
            }
            else
            {
                throw new NotSupportedException(expression.ToString());
            }
        }

        private static object GetValue(MemberExpression exp)
        {
            // expression is ConstantExpression or FieldExpression
            if (exp.Expression is ConstantExpression)
            {
                return (((ConstantExpression)exp.Expression).Value)
                        .GetType()
                        .GetField(exp.Member.Name)
                        .GetValue(((ConstantExpression)exp.Expression).Value);
            }
            else if (exp.Expression is MemberExpression)
            {
                return GetValue((MemberExpression)exp.Expression);
            }
            else
            {
                throw new NotImplementedException();
            }
        }
        #endregion
    }
}
