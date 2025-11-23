namespace CodeLogic.Logging;

/// <summary>
/// Log levels for filtering.
/// </summary>
public enum LogLevel
{
    Trace = 0,
    Debug = 1,
    Info = 2,
    Warning = 3,
    Error = 4,
    Critical = 5
}

/// <summary>
/// Logger interface for libraries and plugins.
/// </summary>
public interface ILogger
{
    /// <summary>
    /// Trace level logging (most verbose).
    /// </summary>
    void Trace(string message);

    /// <summary>
    /// Debug level logging.
    /// </summary>
    void Debug(string message);

    /// <summary>
    /// Info level logging.
    /// </summary>
    void Info(string message);

    /// <summary>
    /// Warning level logging.
    /// </summary>
    void Warning(string message);

    /// <summary>
    /// Error level logging.
    /// </summary>
    void Error(string message, Exception? exception = null);

    /// <summary>
    /// Critical level logging.
    /// </summary>
    void Critical(string message, Exception? exception = null);
}
