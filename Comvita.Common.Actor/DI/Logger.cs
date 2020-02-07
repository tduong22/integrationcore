using Autofac;
using Integration.Common.Logging;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using Microsoft.Extensions.Options;

namespace Comvita.Common.Actor.DI
{
    public class ApplicationInsightLoggerModule : Module
    {
        public string SystemKey { get; set; }
        public string BusinessKey { get; set; }
        public string LogLevel { get; set; }

        protected override void Load(ContainerBuilder builder)
        {
            var loggerConfig = new LoggerConfiguration();
            loggerConfig.SystemInstrumentationKey = SystemKey;
            loggerConfig.BusinessInstrumentationKey = BusinessKey;
            loggerConfig.Level = LogLevel;

            LoggerFilterOptions filterOptions = new LoggerFilterOptions();
            filterOptions.AddFilter("", loggerConfig.LogLevel);
            var config = TelemetryConfiguration.CreateDefault();

            config.InstrumentationKey = loggerConfig.BusinessInstrumentationKey;
            IOptions<TelemetryConfiguration> telemeryOptions = Options.Create(config);
            IOptions<ApplicationInsightsLoggerOptions> configureApplicationInsightsLoggerOptions = Options.Create(
                new ApplicationInsightsLoggerOptions());

            ILoggerFactory loggerFactory =
                new LoggerFactory(
                    new[]
                    {
                        new ApplicationInsightsLoggerProvider(telemeryOptions,
                            configureApplicationInsightsLoggerOptions)
                    }, filterOptions);

            ILogger logger = loggerFactory.CreateLogger("Global_Logger");

            builder.RegisterInstance(loggerConfig);
            builder.RegisterInstance<ILogger>(logger);
            builder.RegisterInstance(loggerFactory)
                .As<ILoggerFactory>();

        }
    }
}
