namespace CodeLogic.Utilities;

/// <summary>
/// Validates system readiness before application startup.
/// Performs pre-flight checks to catch errors early.
/// </summary>
public class StartupValidator
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>
    /// Validates framework configuration and environment.
    /// </summary>
    public ValidationResult Validate(string rootDirectory)
    {
        _errors.Clear();
        _warnings.Clear();

        ValidateDirectories(rootDirectory);
        ValidateSystemRequirements();
        ValidateCodeLogicJson(rootDirectory);

        return new ValidationResult
        {
            IsSuccess = _errors.Count == 0,
            ErrorMessage = _errors.Count > 0 ? string.Join("\n", _errors) : null
        };
    }

    /// <summary>
    /// Validates required directories exist and are writable.
    /// </summary>
    private void ValidateDirectories(string rootDirectory)
    {
        var directories = new[]
        {
            rootDirectory,
            Path.Combine(rootDirectory, "Framework"),
            Path.Combine(rootDirectory, "Framework", "logs")
        };

        foreach (var dir in directories)
        {
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                    _warnings.Add($"Created missing directory: {dir}");
                }

                // Test write access
                var testFile = Path.Combine(dir, ".write_test");
                File.WriteAllText(testFile, "test");
                File.Delete(testFile);
            }
            catch (UnauthorizedAccessException ex)
            {
                _errors.Add($"Directory '{dir}' is not writable: {ex.Message}");
            }
            catch (IOException ex)
            {
                _errors.Add($"I/O error accessing directory '{dir}': {ex.Message}");
            }
            catch (Exception ex)
            {
                _errors.Add($"Failed to access directory '{dir}': {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Validates system requirements (.NET version, memory, etc.).
    /// </summary>
    private void ValidateSystemRequirements()
    {
        // Check .NET version
        var version = Environment.Version;
        if (version.Major < 10)
        {
            _warnings.Add($"Running on .NET {version}. Framework is optimized for .NET 10+");
        }

        // Check available memory (warn if high usage)
        try
        {
            var gcMemory = GC.GetTotalMemory(false);
            if (gcMemory > 1_000_000_000) // > 1GB
            {
                _warnings.Add($"High memory usage detected: {gcMemory / 1_000_000}MB");
            }
        }
        catch
        {
            // Ignore memory check errors
        }

        // Check disk space (warn if low)
        try
        {
            var drive = new DriveInfo(Path.GetPathRoot(Environment.CurrentDirectory) ?? "C:\\");
            if (drive.IsReady && drive.AvailableFreeSpace < 100_000_000) // < 100MB
            {
                _warnings.Add($"Low disk space: {drive.AvailableFreeSpace / 1_000_000}MB available");
            }
        }
        catch
        {
            // Ignore disk space check errors
        }
    }

    /// <summary>
    /// Validates CodeLogic.json exists and is valid JSON.
    /// </summary>
    private void ValidateCodeLogicJson(string rootDirectory)
    {
        var configPath = Path.Combine(rootDirectory, "CodeLogic.json");

        if (!File.Exists(configPath))
        {
            _errors.Add($"CodeLogic.json not found at: {configPath}");
            return;
        }

        try
        {
            var json = File.ReadAllText(configPath);
            System.Text.Json.JsonDocument.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            _errors.Add($"CodeLogic.json contains invalid JSON: {ex.Message}");
        }
        catch (Exception ex)
        {
            _errors.Add($"Failed to read CodeLogic.json: {ex.Message}");
        }
    }

    /// <summary>
    /// Gets validation warnings (non-critical issues).
    /// </summary>
    public IReadOnlyList<string> GetWarnings() => _warnings;

    /// <summary>
    /// Gets validation errors (critical issues).
    /// </summary>
    public IReadOnlyList<string> GetErrors() => _errors;
}

/// <summary>
/// Result of startup validation.
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Indicates whether startup validation succeeded.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Error message when validation fails.
    /// </summary>
    public string? ErrorMessage { get; init; }
}
