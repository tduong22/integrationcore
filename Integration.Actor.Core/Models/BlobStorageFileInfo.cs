using System.Runtime.Serialization;

namespace Integration.Common.Model
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
