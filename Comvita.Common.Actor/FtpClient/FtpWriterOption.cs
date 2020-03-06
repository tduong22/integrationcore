using System.Runtime.Serialization;

namespace Comvita.Common.Actor.FtpClient
{
    [DataContract]
    public class FtpWriterOption : FtpOption
    {
        [DataMember]
        public string WriteData { get; set; }


        [DataMember]
        public string FileName { get; set; }

        public FtpWriterOption(FtpConfig ftpConfig, string writeData, string fileName) : base(ftpConfig)
        {
            WriteData = writeData;
            FileName = fileName;
        }
    }
}