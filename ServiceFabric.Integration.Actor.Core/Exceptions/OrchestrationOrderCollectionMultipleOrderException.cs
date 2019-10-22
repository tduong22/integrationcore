using System;

namespace Integration.Common.Exceptions
{
    public class OrchestrationOrderCollectionMultipleOrderException : Exception
    {
        public OrchestrationOrderCollectionMultipleOrderException(string message) : base(message)
        {

        }
    }
}