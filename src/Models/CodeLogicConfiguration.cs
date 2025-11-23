using CodeLogic.Logging;

namespace CodeLogic.Models;

/// <summary>
/// Global CodeLogic.json configuration.
/// </summary>
public class CodeLogicConfiguration
{
    public FrameworkConfig Framework { get; set; } = new();
    public LoggingConfig Logging { get; set; } = new();
    public LocalizationConfig Localization { get; set; } = new();
    public LibrariesConfig Libraries { get; set; } = new();
}

/// <summary>
/// Framework configuration.
/// </summary>
public class FrameworkConfig
{
    public string Name { get; set; } = "CodeLogic";
    public string Version { get; set; } = "3.0.0";
    public string RootDirectory { get; set; } = "CodeLogic";
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingConfig
{
    public string GlobalLevel { get; set; } = "Info";
    public bool EnableDebugMode { get; set; } = false;
    public bool CentralizedDebugLog { get; set; } = false;
    public string FileNamePattern { get; set; } = "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";
    public bool IncludeTimestamps { get; set; } = true;
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";
    public bool EnableConsoleOutput { get; set; } = true;
    public string ConsoleMinimumLevel { get; set; } = "Info";

    public LogLevel GetGlobalLogLevel()
    {
        return Enum.TryParse<LogLevel>(GlobalLevel, true, out var level) ? level : LogLevel.Info;
    }

    public LogLevel GetConsoleLogLevel()
    {
        return Enum.TryParse<LogLevel>(ConsoleMinimumLevel, true, out var level) ? level : LogLevel.Info;
    }
}

/// <summary>
/// Localization configuration.
/// </summary>
public class LocalizationConfig
{
    public string DefaultCulture { get; set; } = "en-US";
    public List<string> SupportedCultures { get; set; } = new() { "en-US" };
    public bool AutoGenerateTemplates { get; set; } = true;
}

/// <summary>
/// Libraries configuration.
/// </summary>
public class LibrariesConfig
{
    public bool AutoDiscover { get; set; } = true;
    public string DiscoveryPattern { get; set; } = "CL.*";
    public bool AutoLoad { get; set; } = true;
    public bool EnableDependencyResolution { get; set; } = true;
}
