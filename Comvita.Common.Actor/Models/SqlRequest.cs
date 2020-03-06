using System.Collections.Generic;
using System.Data.Common;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class SqlRequest
    {
        [DataMember]
        public string ConnectionString { get; set; }

        [DataMember]
        public List<DbParameter> Parameters { get; set; }

        [DataMember]
        public string StoredProcedureName { get; set; }

        [DataMember]
        public string Domain { get; set; }

        public SqlRequest(string connectionString, List<DbParameter> dbParameters, string spName, string domain) {
            ConnectionString = connectionString;
            Parameters = dbParameters;
            StoredProcedureName = spName;
            Domain = domain;
        }

        public SqlRequest()
        {
        }
    }
}
