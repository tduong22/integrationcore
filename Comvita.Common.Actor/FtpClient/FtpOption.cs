using System.Collections.Generic;
using Comvita.Common.Actor.Interfaces;
using Newtonsoft.Json;
using System.Runtime.Serialization;

namespace Comvita.Common.Actor.FtpClient
{
    /// <summary>
    /// Wrapper for ftpconfig
    /// </summary>
    [DataContract]
    public class FtpOption : IPartitionable
    {
        [JsonProperty(PropertyName = "ftpConfig")]
        [DataMember]
        public FtpConfig FtpConfig { get; set; }

        [JsonProperty(PropertyName = "domain")]
        [DataMember]
        public string Domain { get; set; }

        public string ExtractPartitionKey()
        {
            return Domain;
        }

        public FtpOption(FtpConfig ftpConfig)
        {
            FtpConfig = ftpConfig;
        }
    }
    public class FtpOptionComparer : IEqualityComparer<FtpOption>
    {
        public bool Equals(FtpOption x, FtpOption y)
        {
            if (x?.FtpConfig.Ftp.Host == y?.FtpConfig.Ftp.Host && x.FtpConfig.Ftp.Path == y.FtpConfig.Ftp.Path &&
                x.FtpConfig.Freq == y.FtpConfig.Freq)
            {
                return true;
            }

            return false;
        }

        public int GetHashCode(FtpOption obj)
        {
            return (obj.FtpConfig.Ftp.Host + obj.FtpConfig.Ftp.Path).GetHashCode();
        }
    }
}
