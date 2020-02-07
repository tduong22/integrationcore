using System;

namespace Comvita.Common.Actor.Exceptions
{
    public class ActorSerializationException : Exception
    {
        public ActorSerializationException(string message, Exception innerEx, Type failedType) : base(message + $" {failedType.FullName}", innerEx)
        {

        }
    }
}
