using System.Runtime.Serialization;

namespace Comvita.Common.Actor.Models
{
    [DataContract]
    public class BlobStorageFileInfo
    {
        [DataMember]
        public string FileName { get; set; }

        [DataMember]
        public string Container { get; set; }

        [DataMember] 
        public string SourceFilePath { get; set; }
    }
}
