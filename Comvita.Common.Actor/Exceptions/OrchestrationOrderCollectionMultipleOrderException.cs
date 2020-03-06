using System;

namespace Comvita.Common.Actor.Exceptions
{
    public class OrchestrationOrderCollectionMultipleOrderException : Exception
    {
        public OrchestrationOrderCollectionMultipleOrderException(string message) : base(message)
        {

        }
    }
}