using System;

namespace Integration.Common.Exceptions
{
    public class ActorRequestActionNameNotSupportedException : Exception
    {
        public ActorRequestActionNameNotSupportedException(string message) : base(message)
        {

        }
    }
}
