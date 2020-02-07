using System.Collections.Generic;

namespace Integration.Common.Models
{
    public class ODataCollectionWrapper<T> where T : class
    {
        public IEnumerable<T> Value { get; set; }

        public ErrorRequesetYouForce Error { get; set; }
    }

    public class ErrorRequesetYouForce
    {
        public string Message { get; set; }

        public string CorrelationId { get; set; }

        public string IssuedAt { get; set; }

        public string ErrorCode { get; set; }
    }
}
