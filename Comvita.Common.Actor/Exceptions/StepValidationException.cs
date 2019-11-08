using System;

namespace Comvita.Common.Actor.Exceptions
{
    public class StepValidationException : Exception
    {
        public StepValidationException(string stepName, string message) : base($"{stepName} failed in flow validation phase because of error: {message}")
        {
        }
    }
}
