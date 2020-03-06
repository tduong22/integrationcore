using Dapper;

namespace Comvita.Common.Actor.Models
{
    public class SqlDapperRequest
    {
        public string QueryString { get; set; }

        public DynamicParameters Params { get; set; }
    }
}
