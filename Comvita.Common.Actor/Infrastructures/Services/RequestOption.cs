using System.Collections.Generic;

namespace Comvita.Common.Actor.Infrastructures.Services
{
    public class RequestOption
    {
        public string RequestName { get; set; }

        public Dictionary<string, string> Tokens { get; set; }
    }
}