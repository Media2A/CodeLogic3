using System.Text.Json;
using CodeLogic.Abstractions;
using CodeLogic.Models;
using CodeLogic.Utilities;

namespace CodeLogic;

/// <summary>
/// Static CodeLogic 3.0 framework API.
/// Main entry point for initializing and managing the framework.
/// </summary>
public static class CodeLogic
{
    private static CodeLogicOptions? _options;
    private static CodeLogicConfiguration? _config;
    private static global::CodeLogic.Libraries.LibraryManager? _libraryManager;
    private static bool _initialized = false;

    /// <summary>
    /// Initializes the CodeLogic framework.
    /// Detects first run and performs initialization if needed.
    /// </summary>
    public static async Task<InitializationResult> InitializeAsync(Action<CodeLogicOptions>? configure = null)
    {
        if (_initialized)
        {
            return new InitializationResult
            {
                Success = false,
                IsFirstRun = false,
                ShouldExit = false,
                Message = "CodeLogic already initialized"
            };
        }

        // Setup options
        _options = new CodeLogicOptions();
        configure?.Invoke(_options);

        // Check for first run
        if (FirstRunManager.IsFirstRun(_options))
        {
            Console.WriteLine();
            var firstRunResult = await FirstRunManager.InitializeAsync(_options);

            return new InitializationResult
            {
                Success = firstRunResult.Success,
                IsFirstRun = true,
                ShouldExit = firstRunResult.ShouldExit,
                Message = firstRunResult.Success
                    ? "First-run initialization completed. Please configure and restart."
                    : $"First-run initialization failed: {firstRunResult.Error}"
            };
        }

        // Load CodeLogic.json
        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("   CodeLogic 3.0 Framework");
        Console.WriteLine("════════════════════════════════════════════════════════\n");

        Console.WriteLine("→ Initializing framework...");

        try
        {
            await LoadConfigurationAsync();
            Console.WriteLine("  ✓ Loaded CodeLogic.json");

            _initialized = true;

            Console.WriteLine("✓ Framework initialized\n");

            return new InitializationResult
            {
                Success = true,
                IsFirstRun = false,
                ShouldExit = false,
                Message = "Framework initialized successfully"
            };
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Initialization failed: {ex.Message}\n");
            Console.ResetColor();

            return new InitializationResult
            {
                Success = false,
                IsFirstRun = false,
                ShouldExit = true,
                Message = $"Initialization failed: {ex.Message}"
            };
        }
    }

    /// <summary>
    /// Configures all CL.* libraries.
    /// Discovers, loads, and configures libraries.
    /// </summary>
    public static async Task ConfigureAsync()
    {
        EnsureInitialized();

        // Run startup validation
        Console.WriteLine("→ Validating system...");
        var validator = new Utilities.StartupValidator();
        var rootDirectory = _options!.RootDirectory ?? throw new InvalidOperationException("RootDirectory must not be null.");
        var validationResult = validator.Validate(rootDirectory);

        if (!validationResult.IsSuccess)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("  ✗ Startup validation failed:");
            Console.WriteLine($"  {validationResult.ErrorMessage}");
            Console.ResetColor();
            throw new InvalidOperationException($"Startup validation failed: {validationResult.ErrorMessage}");
        }

        // Show warnings if any
        var warnings = validator.GetWarnings();
        if (warnings.Count > 0)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            foreach (var warning in warnings)
            {
                Console.WriteLine($"  ⚠ {warning}");
            }
            Console.ResetColor();
        }

        Console.WriteLine("  ✓ System validation passed\n");

        Console.WriteLine("→ Discovering libraries...");

        _libraryManager = new global::CodeLogic.Libraries.LibraryManager(_options!, _config!);

        var libraryPaths = _libraryManager.Discover();

        if (libraryPaths.Count == 0)
        {
            Console.WriteLine("  ⚠ No libraries found in CodeLogic directory");
            Console.WriteLine("  ℹ️  Place CL.* libraries in the CodeLogic/ directory\n");
            return;
        }

        await _libraryManager.LoadLibrariesAsync(libraryPaths);

        Console.WriteLine($"\n→ Configuring {libraryPaths.Count} librar{(libraryPaths.Count == 1 ? "y" : "ies")}...");
        await _libraryManager.ConfigureAllAsync();

        Console.WriteLine("✓ Libraries configured\n");
    }

    /// <summary>
    /// Starts all configured libraries.
    /// </summary>
    public static async Task StartAsync()
    {
        EnsureInitialized();

        if (_libraryManager == null)
        {
            Console.WriteLine("⚠ No libraries to start. Call ConfigureAsync() first.\n");
            return;
        }

        Console.WriteLine("→ Initializing libraries...");
        await _libraryManager.InitializeAllAsync();

        Console.WriteLine($"\n→ Starting libraries...");
        await _libraryManager.StartAllAsync();

        Console.WriteLine("✓ All libraries started\n");
    }

    /// <summary>
    /// Stops all running libraries.
    /// </summary>
    public static async Task StopAsync()
    {
        if (_libraryManager == null)
            return;

        Console.WriteLine("\n→ Stopping libraries...");
        await _libraryManager.StopAllAsync();

        Console.WriteLine("✓ All libraries stopped\n");
    }

    /// <summary>
    /// Gets the framework configuration.
    /// </summary>
    public static CodeLogicConfiguration GetConfiguration()
    {
        EnsureInitialized();
        return _config!;
    }

    /// <summary>
    /// Gets the framework options.
    /// </summary>
    public static CodeLogicOptions GetOptions()
    {
        EnsureInitialized();
        return _options!;
    }

    private static async Task LoadConfigurationAsync()
    {
        var configPath = _options!.GetCodeLogicConfigPath();

        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"CodeLogic.json not found at: {configPath}");
        }

        var json = await File.ReadAllTextAsync(configPath);
        _config = JsonSerializer.Deserialize<CodeLogicConfiguration>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        if (_config == null)
        {
            throw new InvalidOperationException("Failed to deserialize CodeLogic.json");
        }
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("CodeLogic not initialized. Call InitializeAsync() first.");
        }
    }

    /// <summary>
    /// Gets the library manager for accessing libraries.
    /// </summary>
    internal static global::CodeLogic.Libraries.LibraryManager? GetLibraryManager() => _libraryManager;
}

/// <summary>
/// Static class for accessing loaded libraries.
/// Usage: Libraries.Get<MySQL2Library>()
/// </summary>
public static class Libs
{
    /// <summary>
    /// Gets a library by type.
    /// </summary>
    public static T? Get<T>() where T : class, ILibrary
    {
        var manager = CodeLogic.GetLibraryManager();
        if (manager == null)
        {
            throw new InvalidOperationException("No libraries loaded. Call CodeLogic.ConfigureAsync() first.");
        }

        return manager.GetLibrary<T>();
    }

    /// <summary>
    /// Manually loads a specific library by type.
    /// Use this for explicit library loading instead of auto-discovery.
    /// Example: await Libs.LoadAsync<MySQL2Library>();
    /// </summary>
    public static async Task<bool> LoadAsync<T>() where T : class, ILibrary, new()
    {
        var manager = CodeLogic.GetLibraryManager();
        if (manager == null)
        {
            throw new InvalidOperationException("CodeLogic not initialized. Call InitializeAsync() first.");
        }

        return await manager.LoadLibraryAsync<T>();
    }

    /// <summary>
    /// Gets a library by ID.
    /// </summary>
    public static ILibrary? Get(string libraryId)
    {
        var manager = CodeLogic.GetLibraryManager();
        if (manager == null)
        {
            throw new InvalidOperationException("No libraries loaded. Call CodeLogic.ConfigureAsync() first.");
        }

        return manager.GetLibrary(libraryId);
    }

    /// <summary>
    /// Gets all loaded libraries.
    /// </summary>
    public static IEnumerable<ILibrary> GetAll()
    {
        var manager = CodeLogic.GetLibraryManager();
        if (manager == null)
        {
            return Enumerable.Empty<ILibrary>();
        }

        return manager.GetAllLibraries();
    }
}

/// <summary>
/// Result of framework initialization.
/// </summary>
public class InitializationResult
{
    /// <summary>
    /// Indicates whether initialization completed successfully.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Indicates whether the current run is the first run initialization path.
    /// </summary>
    public bool IsFirstRun { get; set; }

    /// <summary>
    /// Indicates whether the host should exit after initialization.
    /// </summary>
    public bool ShouldExit { get; set; }

    /// <summary>
    /// Human-readable status or error message from initialization.
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
