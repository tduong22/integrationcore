using System;
using System.Collections.Generic;

namespace Comvita.Common.Actor.Models
{
    public class SplitResult<T>
    {
       public List<T> ListOfSuccess {get;set; }
       public List<T> ListOfFailed {get;set; }
       public List<Exception> ListOfException {get;set; }

        public SplitResult()
        {
            ListOfSuccess = new List<T>();
            ListOfFailed = new List<T>();
            ListOfException = new List<Exception>();
        }
    }

    public class SplitResult : SplitResult<object>
    {
        public SplitResult() : base()
        {
          
        }
    }
}
