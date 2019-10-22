using System;

namespace Integration.Common.Exceptions
{
    public class ActorRequestInvalidException : Exception
    {
        public ActorRequestInvalidException(string message) : base(message)
        {

        }
    }
}
