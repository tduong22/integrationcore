using System;

namespace Comvita.Common.Actor.Exceptions
{
    public class ActorRequestInvalidException : Exception
    {
        public ActorRequestInvalidException(string message) : base(message)
        {

        }
    }
}
