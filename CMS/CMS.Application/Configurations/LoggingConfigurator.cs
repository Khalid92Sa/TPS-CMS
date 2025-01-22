using CMS.Application.CustomLogsEnricher;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Serilog;
using System;
using System.IO;

namespace CMS.Application.Configrations;

public static class LoggingConfigurator
{
    public static IHostBuilder SetupConfiguration(this IHostBuilder hostBuilder, string projectName)
    {
        string environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

        IConfigurationRoot configuration = new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
                                                                     .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                                                                     .AddJsonFile(Path.Combine("LogSettings", "appsettings.LogsConfig.json"), optional: true, reloadOnChange: true)
                                                                     .AddJsonFile(Path.Combine("LogSettings", $"appsettings.LogsConfig.{environment}.json"), optional: true, reloadOnChange: true)
                                                                     .Build();

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(configuration)
            .Enrich.With(new LevelEmojiEnricher())
            .Enrich.WithCorrelationId()
            .CreateLogger();

        return hostBuilder.UseSerilog();
    }
}