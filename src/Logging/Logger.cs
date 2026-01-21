namespace CodeLogic.Logging;

/// <summary>
/// Logger implementation with dual output:
/// 1. Per-library/plugin logs in their own directories
/// 2. Centralized debug log with all libraries combined (when debug mode enabled)
/// </summary>
public class Logger : ILogger
{
    private readonly string _componentName;
    private readonly string _componentLogsPath;
    private readonly LogLevel _minimumLevel;
    private readonly LoggingOptions _options;
    private static readonly object _fileLock = new();

    /// <summary>
    /// Creates a logger scoped to a component and its log directory.
    /// </summary>
    /// <param name="componentName">Name of the component emitting logs.</param>
    /// <param name="componentLogsPath">Directory where component logs are written.</param>
    /// <param name="minimumLevel">Minimum level written to component logs.</param>
    /// <param name="options">Logging options controlling output behavior.</param>
    public Logger(
        string componentName,
        string componentLogsPath,
        LogLevel minimumLevel,
        LoggingOptions options)
    {
        _componentName = componentName;
        _componentLogsPath = componentLogsPath;
        _minimumLevel = minimumLevel;
        _options = options;

        // Ensure logs directory exists
        Directory.CreateDirectory(_componentLogsPath);
    }

    /// <summary>
    /// Logs a trace-level message.
    /// </summary>
    public void Trace(string message) => Log(LogLevel.Trace, message);

    /// <summary>
    /// Logs a debug-level message.
    /// </summary>
    public void Debug(string message) => Log(LogLevel.Debug, message);

    /// <summary>
    /// Logs an informational message.
    /// </summary>
    public void Info(string message) => Log(LogLevel.Info, message);

    /// <summary>
    /// Logs a warning message.
    /// </summary>
    public void Warning(string message) => Log(LogLevel.Warning, message);

    /// <summary>
    /// Logs an error message with an optional exception.
    /// </summary>
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);

    /// <summary>
    /// Logs a critical error message with an optional exception.
    /// </summary>
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);

    private void Log(LogLevel level, string message, Exception? exception = null)
    {
        var timestamp = DateTime.Now.ToString(_options.TimestampFormat ?? "yyyy-MM-dd HH:mm:ss.fff");
        var logEntry = FormatLogEntry(timestamp, level, message, exception, includeComponentName: false);
        var centralizedEntry = FormatLogEntry(timestamp, level, message, exception, includeComponentName: true);

        // Write to component-specific log file (if level >= minimum)
        if (level >= _minimumLevel)
        {
            var componentLogFile = GetComponentLogFile(level);
            WriteToFile(componentLogFile, logEntry);
        }

        // Write to centralized debug log (if enabled, ALL levels)
        if (_options.EnableDebugMode && _options.CentralizedDebugLog)
        {
            var centralizedLogFile = GetCentralizedDebugLogFile();
            WriteToFile(centralizedLogFile, centralizedEntry);
        }

        // Console output (if enabled and level >= console minimum)
        if (_options.EnableConsoleOutput && level >= _options.ConsoleMinimumLevel)
        {
            WriteToConsole(level, timestamp, message, exception);
        }
    }

    private string FormatLogEntry(string timestamp, LogLevel level, string message, Exception? exception, bool includeComponentName)
    {
        var levelStr = level.ToString().ToUpper();
        var entry = includeComponentName
            ? $"{timestamp} [{_componentName}] [{levelStr}] {message}"
            : $"{timestamp} [{levelStr}] {message}";

        if (exception != null)
        {
            entry += $"\n{exception}";
        }

        return entry;
    }

    private string GetComponentLogFile(LogLevel level)
    {
        // Example: CodeLogic/CL.MySQL/logs/2025/11/22/info.log
        var date = DateTime.Now;
        var pattern = _options.FileNamePattern ?? "{date:yyyy}/{date:MM}/{date:dd}/{level}.log";

        // Parse pattern
        var logDir = pattern
            .Replace("{date:yyyy}", date.ToString("yyyy"))
            .Replace("{date:MM}", date.ToString("MM"))
            .Replace("{date:dd}", date.ToString("dd"))
            .Replace("{level}", level.ToString().ToLower());

        // Remove filename from pattern to get directory
        var fileName = Path.GetFileName(logDir);
        var directory = Path.GetDirectoryName(logDir) ?? string.Empty;

        var fullDir = Path.Combine(_componentLogsPath, directory);
        Directory.CreateDirectory(fullDir);

        return Path.Combine(fullDir, fileName);
    }

    private string GetCentralizedDebugLogFile()
    {
        // Example: CodeLogic/Framework/logs/2025/11/22/debug_all.log
        var date = DateTime.Now;
        var frameworkLogsPath = Path.Combine(
            Directory.GetParent(_componentLogsPath)?.Parent?.FullName ?? AppDomain.CurrentDomain.BaseDirectory,
            "Framework",
            "logs"
        );

        var logDir = Path.Combine(
            frameworkLogsPath,
            date.ToString("yyyy"),
            date.ToString("MM"),
            date.ToString("dd")
        );

        Directory.CreateDirectory(logDir);
        return Path.Combine(logDir, "debug_all.log");
    }

    private void WriteToFile(string filePath, string content)
    {
        lock (_fileLock)
        {
            try
            {
                File.AppendAllText(filePath, content + Environment.NewLine);
            }
            catch (Exception ex)
            {
                // Fallback: write to console if file write fails
                Console.Error.WriteLine($"Failed to write to log file: {ex.Message}");
                Console.Error.WriteLine(content);
            }
        }
    }

    private void WriteToConsole(LogLevel level, string timestamp, string message, Exception? exception)
    {
        var consoleColor = GetConsoleColor(level);
        var levelStr = level.ToString().ToUpper();

        lock (Console.Out)
        {
            Console.ForegroundColor = consoleColor;
            Console.WriteLine($"[{_componentName}] {timestamp} [{levelStr}] {message}");

            if (exception != null)
            {
                Console.WriteLine(exception.ToString());
            }

            Console.ResetColor();
        }
    }

    private ConsoleColor GetConsoleColor(LogLevel level)
    {
        return level switch
        {
            LogLevel.Trace => ConsoleColor.DarkGray,
            LogLevel.Debug => ConsoleColor.Cyan,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            LogLevel.Critical => ConsoleColor.DarkRed,
            _ => ConsoleColor.White
        };
    }
}

/// <summary>
/// Logging configuration options.
/// </summary>
public class LoggingOptions
{
    /// <summary>
    /// Global minimum log level for component logs.
    /// </summary>
    public LogLevel GlobalLevel { get; set; } = LogLevel.Info;

    /// <summary>
    /// Enables debug mode and optional centralized logging.
    /// </summary>
    public bool EnableDebugMode { get; set; } = false;

    /// <summary>
    /// Writes all log entries to a centralized debug log when enabled.
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
    public LogLevel ConsoleMinimumLevel { get; set; } = LogLevel.Info;
}
