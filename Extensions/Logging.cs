using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace EVerland.Extentions;

public static class LoggingExtension
{
    public static WebApplicationBuilder AddLocalFileLogging(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection(LocalFileLogOptions.SectionName).Get<LocalFileLogOptions>()
            ?? new LocalFileLogOptions();

        var loggerConfiguration = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .WriteTo.Console(new RenderedCompactJsonFormatter());

        if (options.Enabled)
        {
            loggerConfiguration.WriteTo.File(
                formatter: new RenderedCompactJsonFormatter(),
                path: options.Path,
                rollingInterval: RollingInterval.Day,
                restrictedToMinimumLevel: ResolveMinimumLevel(options.MinimumLevel),
                retainedFileCountLimit: options.RetainedFileCountLimit,
                shared: true);
        }

        Log.Logger = loggerConfiguration.CreateLogger();
        builder.Host.UseSerilog();

        return builder;
    }

    private static LogEventLevel ResolveMinimumLevel(string minimumLevel)
    {
        return minimumLevel.Trim().ToLowerInvariant() switch
        {
            "verbose" => LogEventLevel.Verbose,
            "debug" => LogEventLevel.Debug,
            "information" => LogEventLevel.Information,
            "warning" => LogEventLevel.Warning,
            "error" => LogEventLevel.Error,
            "fatal" => LogEventLevel.Fatal,
            _ => LogEventLevel.Warning
        };
    }
}

public sealed class LocalFileLogOptions
{
    public const string SectionName = "Logging:LocalFile";

    public bool Enabled { get; set; } = true;
    public string Path { get; set; } = "logs/e-verland-.ndjson";
    public string MinimumLevel { get; set; } = "Warning";
    public int RetainedFileCountLimit { get; set; } = 14;
}