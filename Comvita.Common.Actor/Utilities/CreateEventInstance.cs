using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Comvita.Common.Actor.Models;
using Comvita.Common.EventBus.Events;

namespace Comvita.Common.Actor.Utilities
{
    public delegate IntegrationEvent ObjectActivator(params object[] args);

    public static class InstanceUtilities
    {
        /// <summary>
        /// Create new Integration Event
        /// </summary>
        /// <param name="data"></param>
        /// <param name="eventName"></param>
        /// <param name="typeOfEvent"></param>
        /// <returns></returns>
        public static IntegrationEvent CreateEventInstance(object data, Type typeOfEvent)
        {
            try
            {
                //find the contructor with one parameter which take object data type
                bool isCorrectConstructor(ConstructorInfo c)
                {
                    var parameters = c.GetParameters();
                    if (parameters.Length == 1)
                    {
                        var firstParameter = parameters.FirstOrDefault();
                        if (firstParameter?.ParameterType == data.GetType())
                        {
                            return true;
                        }
                    }

                    return false;
                }

                ConstructorInfo ctor = (typeOfEvent.GetConstructors().First(isCorrectConstructor));
                ObjectActivator createdActivator = GetActivator(ctor);
                IntegrationEvent instance = createdActivator(data);
                return instance;
            }
            catch (Exception)
            {
                return new DefaultIntegrationEvent(data, typeOfEvent.Name);
            }
        }

        public static ObjectActivator GetActivator
            (ConstructorInfo ctor)
        {
            Type type = ctor.DeclaringType;
            ParameterInfo[] paramsInfo = ctor.GetParameters();                  

            //create a single param of type object[]
            ParameterExpression param =
                Expression.Parameter(typeof(object[]), "args");
 
            Expression[] argsExp =
                new Expression[paramsInfo.Length];            

            //pick each arg from the params array 
            //and create a typed expression of them
            for (int i = 0; i < paramsInfo.Length; i++)
            {
                Expression index = Expression.Constant(i);
                Type paramType = paramsInfo[i].ParameterType;              

                Expression paramAccessorExp =
                    Expression.ArrayIndex(param, index);              

                Expression paramCastExp =
                    Expression.Convert (paramAccessorExp, paramType);              

                argsExp[i] = paramCastExp;
            }                  

            //make a NewExpression that calls the
            //ctor with the args we just created
            NewExpression newExp = Expression.New(ctor,argsExp);                  

            //create a lambda with the New
            //Expression as body and our param object[] as arg
            LambdaExpression lambda =
                Expression.Lambda(typeof(ObjectActivator), newExp, param);              

            //compile it
            ObjectActivator compiled = (ObjectActivator)lambda.Compile();
            return compiled;
        }
    }
}
