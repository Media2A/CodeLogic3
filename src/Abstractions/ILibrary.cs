using CodeLogic.Configuration;
using CodeLogic.Localization;
using CodeLogic.Logging;

namespace CodeLogic.Abstractions;

/// <summary>
/// Interface for CodeLogic internal libraries (CL.* libraries).
/// These are framework-provided libraries that extend CodeLogic functionality.
/// Not hot-pluggable - loaded at application startup.
/// </summary>
public interface ILibrary : IDisposable
{
    /// <summary>
    /// Gets the library manifest with metadata.
    /// </summary>
    LibraryManifest Manifest { get; }

    /// <summary>
    /// Phase 1: Configure
    /// Register configuration and localization models.
    /// Called during framework configuration phase.
    /// </summary>
    Task OnConfigureAsync(LibraryContext context);

    /// <summary>
    /// Phase 2: Initialize
    /// Setup services based on loaded configuration.
    /// Called after all configs/localizations are loaded.
    /// </summary>
    Task OnInitializeAsync(LibraryContext context);

    /// <summary>
    /// Phase 3: Start
    /// Start services, connections, and background workers.
    /// Called when framework is starting up.
    /// </summary>
    Task OnStartAsync(LibraryContext context);

    /// <summary>
    /// Phase 4: Stop
    /// Stop services gracefully.
    /// Called when framework is shutting down.
    /// </summary>
    Task OnStopAsync();

    /// <summary>
    /// Health check for monitoring.
    /// Returns current health status of the library.
    /// </summary>
    Task<HealthStatus> HealthCheckAsync();
}

/// <summary>
/// Represents a dependency on another library with version requirements.
/// </summary>
public record LibraryDependency
{
    /// <summary>
    /// Identifier of the required library (e.g., "mysql", "mail")
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Minimum required version (semantic versioning).
    /// Example: "2.0.0" means the dependency must be >= 2.0.0
    /// If null, any version is acceptable.
    /// </summary>
    public string? MinVersion { get; init; }

    /// <summary>
    /// Whether this dependency is optional.
    /// Optional dependencies are loaded if available, but don't fail startup if missing.
    /// </summary>
    public bool IsOptional { get; init; } = false;

    /// <summary>
    /// Creates a required dependency with no version constraint
    /// </summary>
    public static LibraryDependency Required(string id) => new() { Id = id };

    /// <summary>
    /// Creates a required dependency with minimum version
    /// </summary>
    public static LibraryDependency Required(string id, string minVersion) => new() { Id = id, MinVersion = minVersion };

    /// <summary>
    /// Creates an optional dependency
    /// </summary>
    public static LibraryDependency Optional(string id) => new() { Id = id, IsOptional = true };

    /// <summary>
    /// Creates an optional dependency with minimum version
    /// </summary>
    public static LibraryDependency Optional(string id, string minVersion) => new() { Id = id, MinVersion = minVersion, IsOptional = true };
}

/// <summary>
/// Attribute for defining library dependency (can be applied multiple times).
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class LibraryDependencyAttribute : Attribute
{
    /// <summary>
    /// Identifier of the dependent library.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Minimum required version, or null for any version.
    /// </summary>
    public string? MinVersion { get; set; }

    /// <summary>
    /// Indicates whether the dependency is optional.
    /// </summary>
    public bool IsOptional { get; set; } = false;

    /// <summary>
    /// Builds a <see cref="LibraryDependency"/> from this attribute.
    /// </summary>
    public LibraryDependency ToDependency()
    {
        return new LibraryDependency
        {
            Id = Id,
            MinVersion = MinVersion,
            IsOptional = IsOptional
        };
    }
}

/// <summary>
/// Attribute for defining library manifest metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LibraryManifestAttribute : Attribute
{
    /// <summary>
    /// Library identifier (e.g., "mysql2").
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Display name for the library.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Library version string.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Optional library description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional library author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Optional tags for discovery and cataloging.
    /// </summary>
    public string[]? Tags { get; set; }

    /// <summary>
    /// Creates a <see cref="LibraryManifest"/> for the provided library type.
    /// </summary>
    /// <param name="libraryType">The library type to read attributes from.</param>
    public LibraryManifest ToManifest(Type libraryType)
    {
        // Collect dependency attributes
        var depAttrs = libraryType.GetCustomAttributes(typeof(LibraryDependencyAttribute), false)
            .Cast<LibraryDependencyAttribute>()
            .Select(a => a.ToDependency())
            .ToArray();

        return new LibraryManifest
        {
            Id = Id,
            Name = Name,
            Version = Version,
            Description = Description,
            Author = Author,
            Dependencies = depAttrs,
            Tags = Tags ?? Array.Empty<string>()
        };
    }
}

/// <summary>
/// Library manifest containing metadata.
/// </summary>
public class LibraryManifest
{
    /// <summary>
    /// Library identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Library display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Library version string.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Optional library description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional library author.
    /// </summary>
    public string? Author { get; set; }

    /// <summary>
    /// Declared dependencies for the library.
    /// </summary>
    public LibraryDependency[] Dependencies { get; set; } = Array.Empty<LibraryDependency>();

    /// <summary>
    /// Tags describing the library.
    /// </summary>
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Context provided to each library containing paths and services.
/// Each library gets isolated directories in CodeLogic/CL.{LibraryId}/
/// </summary>
public class LibraryContext
{
    /// <summary>
    /// Library ID (e.g., "mysql", "mail")
    /// </summary>
    public required string LibraryId { get; init; }

    /// <summary>
    /// Root directory for this library
    /// Example: CodeLogic/CL.MySQL/
    /// </summary>
    public required string LibraryDirectory { get; init; }

    /// <summary>
    /// Config file path for this library
    /// Example: CodeLogic/CL.MySQL/config.json
    /// </summary>
    public string ConfigPath => Path.Combine(LibraryDirectory, "config.json");

    /// <summary>
    /// Localization directory for this library
    /// Example: CodeLogic/CL.MySQL/localization/
    /// </summary>
    public string LocalizationDirectory => Path.Combine(LibraryDirectory, "localization");

    /// <summary>
    /// Data directory for this library
    /// Example: CodeLogic/CL.MySQL/data/
    /// </summary>
    public string DataDirectory => Path.Combine(LibraryDirectory, "data");

    /// <summary>
    /// Logs directory for this library
    /// Example: CodeLogic/CL.MySQL/logs/
    /// </summary>
    public string LogsDirectory => Path.Combine(LibraryDirectory, "logs");

    /// <summary>
    /// Logger instance for this library (writes to library's log folder)
    /// </summary>
    public required ILogger Logger { get; init; }

    /// <summary>
    /// Configuration manager (scoped to this library)
    /// </summary>
    public required IConfigurationManager Configuration { get; init; }

    /// <summary>
    /// Localization manager (scoped to this library)
    /// </summary>
    public required ILocalizationManager Localization { get; init; }
}

/// <summary>
/// Health status levels.
/// </summary>
public enum HealthStatusLevel
{
    /// <summary>
    /// The library is operating normally.
    /// </summary>
    Healthy,
    
    /// <summary>
    /// The library is operating but with reduced functionality.
    /// </summary>
    Degraded,
    
    /// <summary>
    /// The library is not functioning properly.
    /// </summary>
    Unhealthy
}

/// <summary>
/// Health status for a library.
/// </summary>
public class HealthStatus
{
    /// <summary>
    /// Overall health status level.
    /// </summary>
    public HealthStatusLevel Status { get; set; }

    /// <summary>
    /// Human-readable health message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Optional structured data associated with the health check.
    /// </summary>
    public Dictionary<string, object>? Data { get; set; }

    /// <summary>
    /// Creates a healthy status.
    /// </summary>
    /// <param name="message">Optional message describing the healthy state.</param>
    public static HealthStatus Healthy(string message = "Healthy")
    {
        return new HealthStatus { Status = HealthStatusLevel.Healthy, Message = message };
    }

    /// <summary>
    /// Creates a degraded status.
    /// </summary>
    /// <param name="message">Message describing the degraded state.</param>
    public static HealthStatus Degraded(string message)
    {
        return new HealthStatus { Status = HealthStatusLevel.Degraded, Message = message };
    }

    /// <summary>
    /// Creates an unhealthy status.
    /// </summary>
    /// <param name="message">Message describing the unhealthy state.</param>
    public static HealthStatus Unhealthy(string message)
    {
        return new HealthStatus { Status = HealthStatusLevel.Unhealthy, Message = message };
    }
}
