namespace CodeLogic.Logging;

/// <summary>
/// Log levels for filtering.
/// </summary>
public enum LogLevel
{
    /// <summary>
    /// Trace level logging (most verbose, for detailed diagnostic information).
    /// </summary>
    Trace = 0,
    
    /// <summary>
    /// Debug level logging (for debugging information).
    /// </summary>
    Debug = 1,
    
    /// <summary>
    /// Info level logging (for informational messages).
    /// </summary>
    Info = 2,
    
    /// <summary>
    /// Warning level logging (for warning messages).
    /// </summary>
    Warning = 3,
    
    /// <summary>
    /// Error level logging (for error messages).
    /// </summary>
    Error = 4,
    
    /// <summary>
    /// Critical level logging (for critical errors that may cause application failure).
    /// </summary>
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
