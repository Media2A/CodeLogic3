using System.Text.Json;
using CodeLogic.Models;

namespace CodeLogic.Utilities;

/// <summary>
/// Manages first-run initialization and setup.
/// Creates a .codelogic file to track initialization state.
/// </summary>
public static class FirstRunManager
{
    private const string FirstRunMarkerFile = ".codelogic";

    /// <summary>
    /// Checks if this is the first run.
    /// </summary>
    public static bool IsFirstRun()
    {
        var markerPath = GetMarkerPath();
        return !File.Exists(markerPath);
    }

    /// <summary>
    /// Performs first-run initialization.
    /// </summary>
    public static async Task<FirstRunResult> InitializeAsync(CodeLogicOptions options)
    {
        var result = new FirstRunResult();

        Console.WriteLine("════════════════════════════════════════════════════════");
        Console.WriteLine("   CodeLogic 3.0 - First Run Initialization");
        Console.WriteLine("════════════════════════════════════════════════════════\n");

        try
        {
            // Create directory structure
            Console.WriteLine("→ Creating directory structure...");
            CreateDirectories(options, result);
            Console.WriteLine($"  ✓ Created {result.DirectoriesCreated} directories\n");

            // Generate default CodeLogic.json
            Console.WriteLine("→ Generating CodeLogic.json...");
            await GenerateDefaultConfigurationAsync(options);
            Console.WriteLine("  ✓ Generated CodeLogic.json\n");

            // Create marker file
            Console.WriteLine("→ Creating initialization marker...");
            await CreateMarkerFileAsync(options);
            Console.WriteLine("  ✓ Created .codelogic file\n");

            result.Success = true;

            Console.WriteLine("════════════════════════════════════════════════════════");
            Console.WriteLine("   Initialization Complete!");
            Console.WriteLine("════════════════════════════════════════════════════════\n");

            Console.WriteLine("📁 Directory Structure:");
            Console.WriteLine($"   {options.GetRootPath()}/");
            Console.WriteLine($"   ├── CodeLogic.json");
            Console.WriteLine($"   ├── Framework/");
            Console.WriteLine($"   │   └── logs/");
            Console.WriteLine($"   ├── CL.*/  (libraries will be auto-discovered here)");
            Console.WriteLine($"   └── .codelogic (initialization marker)\n");

            Console.WriteLine("📝 Next Steps:");
            Console.WriteLine("   1. Review and edit CodeLogic.json if needed");
            Console.WriteLine("   2. Place CL.* libraries in the CodeLogic/ directory");
            Console.WriteLine("   3. Run your application again\n");

            Console.WriteLine("ℹ️  The application will now exit to allow you to configure.");
            Console.WriteLine("   Re-run the application when ready.\n");

            result.ShouldExit = true;
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Error = ex.Message;
            result.ShouldExit = true;

            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n✗ Initialization failed: {ex.Message}\n");
            Console.ResetColor();
        }

        return result;
    }

    /// <summary>
    /// Marks initialization as complete.
    /// </summary>
    public static async Task CompleteInitializationAsync(CodeLogicOptions options)
    {
        await CreateMarkerFileAsync(options);
    }

    /// <summary>
    /// Resets the first-run marker (for testing or re-initialization).
    /// </summary>
    public static void Reset()
    {
        var markerPath = GetMarkerPath();
        if (File.Exists(markerPath))
        {
            File.Delete(markerPath);
        }
    }

    private static void CreateDirectories(CodeLogicOptions options, FirstRunResult result)
    {
        var directories = new[]
        {
            options.GetRootPath(),
            options.GetFrameworkPath(),
            Path.Combine(options.GetFrameworkPath(), "logs"),
            options.GetPluginsPath()
        };

        foreach (var dir in directories)
        {
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                result.DirectoriesCreated++;
            }
        }
    }

    private static async Task GenerateDefaultConfigurationAsync(CodeLogicOptions options)
    {
        var configPath = options.GetCodeLogicConfigPath();

        if (File.Exists(configPath))
            return;

        var defaultConfig = new CodeLogicConfiguration
        {
            Framework = new FrameworkConfig
            {
                Name = "CodeLogic",
                Version = "3.0.0",
                RootDirectory = options.RootDirectory ?? "CodeLogic"
            },
            Logging = new LoggingConfig
            {
                GlobalLevel = "Info",
                EnableDebugMode = false,
                CentralizedDebugLog = false,
                EnableConsoleOutput = true,
                ConsoleMinimumLevel = "Info"
            },
            Localization = new LocalizationConfig
            {
                DefaultCulture = "en-US",
                SupportedCultures = new List<string> { "en-US" },
                AutoGenerateTemplates = true
            },
            Libraries = new LibrariesConfig
            {
                AutoDiscover = true,
                DiscoveryPattern = "CL.*",
                AutoLoad = true,
                EnableDependencyResolution = true
            }
        };

        var json = JsonSerializer.Serialize(defaultConfig, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(configPath, json);
    }

    private static async Task CreateMarkerFileAsync(CodeLogicOptions options)
    {
        var markerPath = GetMarkerPath();
        var markerData = new FirstRunMarker
        {
            InitializedAt = DateTime.UtcNow,
            Version = "3.0.0",
            RootDirectory = options.RootDirectory ?? "CodeLogic"
        };

        var json = JsonSerializer.Serialize(markerData, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });

        await File.WriteAllTextAsync(markerPath, json);
    }

    private static string GetMarkerPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, FirstRunMarkerFile);
    }
}

/// <summary>
/// Result of first-run initialization.
/// </summary>
public class FirstRunResult
{
    public bool Success { get; set; }
    public bool ShouldExit { get; set; }
    public string? Error { get; set; }
    public int DirectoriesCreated { get; set; }
}

/// <summary>
/// Data stored in .codelogic marker file.
/// </summary>
public class FirstRunMarker
{
    public DateTime InitializedAt { get; set; }
    public string Version { get; set; } = "3.0.0";
    public string RootDirectory { get; set; } = "CodeLogic";
}
