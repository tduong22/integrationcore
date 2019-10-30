using System;

namespace Integration.Common.Exceptions
{
    public class StepValidationException : Exception
    {
        public StepValidationException(string stepName, string message) : base($"{stepName} failed in flow validation phase because of error: {message}")
        {
        }
    }
}
