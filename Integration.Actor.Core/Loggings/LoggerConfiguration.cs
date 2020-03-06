using Microsoft.Extensions.Logging;
using System;

namespace Integration.Common.Logging
{
    public class LoggerConfiguration
    {
        public string SystemInstrumentationKey { get; set; }

        public string BusinessInstrumentationKey { get; set; }

        public string Level { get; set; }

        public LogLevel LogLevel => (LogLevel)Enum.Parse(typeof(LogLevel), Level);
    }
}
