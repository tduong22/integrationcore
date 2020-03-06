using System;

namespace Comvita.Common.Actor.Exceptions
{
    public class ActorRequestActionNameNotSupportedException : Exception
    {
        public ActorRequestActionNameNotSupportedException(string message) : base(message)
        {

        }
    }
}
