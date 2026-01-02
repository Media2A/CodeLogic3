using CodeLogic.Logging;

namespace CodeLogic.Models;

/// <summary>
/// Global CodeLogic.json configuration.
/// </summary>
public class CodeLogicConfiguration
{
    /// <summary>
    /// Framework-level settings such as name, version, and root directory.
    /// </summary>
    public FrameworkConfig Framework { get; set; } = new();

    /// <summary>
    /// Logging settings for global and console outputs.
    /// </summary>
    public LoggingConfig Logging { get; set; } = new();

    /// <summary>
    /// Localization settings for default and supported cultures.
    /// </summary>
    public LocalizationConfig Localization { get; set; } = new();

    /// <summary>
    /// Library discovery and dependency resolution settings.
    /// </summary>
    public LibrariesConfig Libraries { get; set; } = new();
}

/// <summary>
/// Framework configuration.
/// </summary>
public class FrameworkConfig
{
    /// <summary>
    /// Display name for the framework.
    /// </summary>
    public string Name { get; set; } = "CodeLogic";

    /// <summary>
    /// Framework version reported to consumers.
    /// </summary>
    public string Version { get; set; } = "3.0.0";

    /// <summary>
    /// Default root directory name for CodeLogic files.
    /// </summary>
    public string RootDirectory { get; set; } = "CodeLogic";
}

/// <summary>
/// Logging configuration.
/// </summary>
public class LoggingConfig
{
    /// <summary>
    /// Global minimum log level name (e.g. Info, Debug).
    /// </summary>
    public string GlobalLevel { get; set; } = "Info";

    /// <summary>
    /// Enables debug mode which may increase verbosity.
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;

    /// <summary>
    /// Enables centralized debug log output under the framework directory.
    /// </summary>
    public bool CentralizedDebugLog { get; set; } = false;

    /// <summary>
    /// File name pattern for log file paths.
    /// </summary>
    public string FileNamePattern { get; set; } = "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";

    /// <summary>
    /// Enables timestamps in log messages.
    /// </summary>
    public bool IncludeTimestamps { get; set; } = true;

    /// <summary>
    /// Timestamp format string for log messages.
    /// </summary>
    public string TimestampFormat { get; set; } = "yyyy-MM-dd HH:mm:ss.fff";

    /// <summary>
    /// Enables console logging output.
    /// </summary>
    public bool EnableConsoleOutput { get; set; } = true;

    /// <summary>
    /// Minimum log level for console output.
    /// </summary>
    public string ConsoleMinimumLevel { get; set; } = "Info";

    /// <summary>
    /// Parses the global log level string into a <see cref="LogLevel"/>.
    /// </summary>
    public LogLevel GetGlobalLogLevel()
    {
        return Enum.TryParse<LogLevel>(GlobalLevel, true, out var level) ? level : LogLevel.Info;
    }

    /// <summary>
    /// Parses the console log level string into a <see cref="LogLevel"/>.
    /// </summary>
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
    /// <summary>
    /// Default culture to use when no culture is specified.
    /// </summary>
    public string DefaultCulture { get; set; } = "en-US";

    /// <summary>
    /// List of supported culture identifiers.
    /// </summary>
    public List<string> SupportedCultures { get; set; } = new() { "en-US" };

    /// <summary>
    /// Indicates whether localization templates are auto-generated.
    /// </summary>
    public bool AutoGenerateTemplates { get; set; } = true;
}

/// <summary>
/// Libraries configuration.
/// </summary>
public class LibrariesConfig
{
    /// <summary>
    /// Enables automatic discovery of libraries in the library directory.
    /// </summary>
    public bool AutoDiscover { get; set; } = true;

    /// <summary>
    /// Search pattern for library folders (e.g. CL.*).
    /// </summary>
    public string DiscoveryPattern { get; set; } = "CL.*";

    /// <summary>
    /// Automatically load discovered libraries.
    /// </summary>
    public bool AutoLoad { get; set; } = true;

    /// <summary>
    /// Enables dependency resolution between libraries.
    /// </summary>
    public bool EnableDependencyResolution { get; set; } = true;
}
