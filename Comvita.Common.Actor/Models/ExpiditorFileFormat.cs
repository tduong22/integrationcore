using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class ExpiditorFileFormat
    {
        [DataMember] public List<ExpiditorRow> ExpiditorRows { get; set; }
    }
    [DataContract]
    public class ExpiditorRow
    {
        [DataMember] public List<string> ExpiditorColumns { get; set; }
    }
}