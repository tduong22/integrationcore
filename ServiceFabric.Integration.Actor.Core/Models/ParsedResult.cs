using System.Collections.Generic;
using System.Linq;
using Integration.Common.Model;

namespace Integration.Common.Model
{
    public class SampleExceptionResponse : ExceptionResponse
    {

    }

    public class ExceptionResponse
    {

    }
}
    public class EmptyResponse
    {

    }

    public class EmptyParsedResult : ParsedResult<EmptyResponse>
    {

    }

    public class ParsedResult<T>
    {
        public bool IsSuccess { get; set; }
        public List<T> Result { get; set; }
        public ExceptionResponse ExceptionResponse { get; set; }

        public ParsedResult()
        {
            Result = new List<T>();
        }

        public ParsedResult<T> SetSuccessResult(T response)
        {
            Result.Add(response);
            IsSuccess = true;
            return this;
        }

        public ParsedResult<T> SetSuccessResult(IEnumerable<T> response)
        {
            Result = response.ToList();
            IsSuccess = true;
            return this;
        }

        public ParsedResult<T> SetFailedResult(ExceptionResponse response)
        {
            ExceptionResponse = response;
            IsSuccess = false;
            return this;
        }
}
