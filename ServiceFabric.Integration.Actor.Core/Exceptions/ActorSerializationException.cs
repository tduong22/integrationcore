using System;

namespace Integration.Common.Exceptions
{
    public class ActorSerializationException : Exception
    {
        public ActorSerializationException(string message, Exception innerEx, Type failedType) : base(message + $" {failedType.FullName}", innerEx)
        {

        }
    }
}
