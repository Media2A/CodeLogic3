using System.Reflection;
using CodeLogic.Abstractions;
using CodeLogic.Configuration;
using CodeLogic.Localization;
using CodeLogic.Logging;
using CodeLogic.Models;

namespace CodeLogic.Libraries;

/// <summary>
/// Manages discovery, loading, and lifecycle of CL.* libraries.
/// </summary>
public class LibraryManager
{
    private readonly CodeLogicOptions _options;
    private readonly CodeLogicConfiguration _config;
    private readonly List<LoadedLibrary> _libraries = new();
    private readonly Dictionary<string, ILibrary> _librariesById = new();

    /// <summary>
    /// Creates a new library manager using the provided options and configuration.
    /// </summary>
    /// <param name="options">Framework options for path resolution.</param>
    /// <param name="config">Framework configuration settings.</param>
    public LibraryManager(CodeLogicOptions options, CodeLogicConfiguration config)
    {
        _options = options;
        _config = config;

        // Add assembly resolver for libraries in subdirectories
        AppDomain.CurrentDomain.AssemblyResolve += OnAssemblyResolve;
    }

    /// <summary>
    /// Discovers all CL.* libraries in the application's root directory.
    /// DLLs are located in the app root folder, while config/data/logs are in CodeLogic/CL.*/ subdirectories.
    /// </summary>
    public List<string> Discover()
    {
        var appBaseDir = AppDomain.CurrentDomain.BaseDirectory;
        var libraryPaths = new List<string>();

        if (!Directory.Exists(appBaseDir))
            return libraryPaths;

        // Find all CL.*.dll files in the application root directory
        var pattern = _config.Libraries.DiscoveryPattern.Replace("*", "*.dll");
        var dllFiles = Directory.GetFiles(appBaseDir, pattern, SearchOption.TopDirectoryOnly);

        foreach (var dllPath in dllFiles)
        {
            libraryPaths.Add(dllPath);
        }

        return libraryPaths;
    }

    /// <summary>
    /// Manually loads a specific library by type.
    /// Use this for explicit library loading instead of auto-discovery.
    /// </summary>
    public async Task<bool> LoadLibraryAsync<T>() where T : class, ILibrary, new()
    {
        try
        {
            var library = new T();
            var manifest = library.Manifest;

            // Check if already loaded
            if (_librariesById.ContainsKey(manifest.Id))
            {
                Console.WriteLine($"  ⚠ Library {manifest.Name} already loaded");
                return false;
            }

            var loadedLibrary = new LoadedLibrary
            {
                Instance = library,
                Manifest = manifest,
                AssemblyPath = typeof(T).Assembly.Location,
                LoadedAt = DateTime.UtcNow
            };

            _libraries.Add(loadedLibrary);
            _librariesById[manifest.Id] = library;

            Console.WriteLine($"  ✓ Manually loaded: {manifest.Name} v{manifest.Version}");
            return true;
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Failed to load library {typeof(T).Name}: {ex.Message}");
            Console.ResetColor();
            return false;
        }
    }

    /// <summary>
    /// Loads libraries from discovered paths.
    /// </summary>
    public async Task LoadLibrariesAsync(List<string> libraryPaths)
    {
        foreach (var path in libraryPaths)
        {
            try
            {
                var assembly = Assembly.LoadFrom(path);
                var libraryType = FindLibraryType(assembly);

                if (libraryType == null)
                {
                    Console.WriteLine($"  ⚠ Warning: No ILibrary implementation found in {Path.GetFileName(path)}");
                    continue;
                }

                var library = (ILibrary)Activator.CreateInstance(libraryType)!;
                var manifest = library.Manifest;

                var loadedLibrary = new LoadedLibrary
                {
                    Instance = library,
                    Manifest = manifest,
                    AssemblyPath = path,
                    LoadedAt = DateTime.UtcNow
                };

                _libraries.Add(loadedLibrary);
                _librariesById[manifest.Id] = library;

                Console.WriteLine($"  ✓ Discovered: {manifest.Name} v{manifest.Version}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Failed to load {Path.GetFileName(path)}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Phase 1: Configure all libraries.
    /// </summary>
    public async Task ConfigureAllAsync()
    {
        // Resolve dependency order
        var orderedLibraries = _config.Libraries.EnableDependencyResolution
            ? ResolveDependencyOrder()
            : _libraries;

        // Validate dependencies (version requirements, missing dependencies, etc.)
        ValidateDependencies(orderedLibraries);

        foreach (var loaded in orderedLibraries)
        {
            var library = loaded.Instance;
            var manifest = library.Manifest;

            try
            {
                // Create library context with isolated paths
                var context = CreateLibraryContext(manifest.Id);

                // Create library directory if it doesn't exist
                Directory.CreateDirectory(context.LibraryDirectory);
                Directory.CreateDirectory(context.DataDirectory);
                Directory.CreateDirectory(context.LogsDirectory);
                Directory.CreateDirectory(context.LocalizationDirectory);

                // Call OnConfigureAsync
                await library.OnConfigureAsync(context);

                // Generate and load configurations
                await context.Configuration.GenerateAllDefaultsAsync();
                await context.Configuration.LoadAllAsync();

                // Generate and load localizations
                await context.Localization.GenerateAllTemplatesAsync(_config.Localization.SupportedCultures);
                await context.Localization.LoadAllAsync(_config.Localization.SupportedCultures);

                // Store context for later use
                loaded.Context = context;

                Console.WriteLine($"  ✓ Configured: {manifest.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Failed to configure {manifest.Name}: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }
    }

    /// <summary>
    /// Phase 2: Initialize all libraries.
    /// </summary>
    public async Task InitializeAllAsync()
    {
        foreach (var loaded in _libraries)
        {
            try
            {
                if (loaded.Context == null)
                    throw new InvalidOperationException("Library not configured");

                await loaded.Instance.OnInitializeAsync(loaded.Context);
                Console.WriteLine($"  ✓ Initialized: {loaded.Manifest.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Failed to initialize {loaded.Manifest.Name}: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }
    }

    /// <summary>
    /// Phase 3: Start all libraries.
    /// </summary>
    public async Task StartAllAsync()
    {
        foreach (var loaded in _libraries)
        {
            try
            {
                if (loaded.Context == null)
                    throw new InvalidOperationException("Library not configured");

                await loaded.Instance.OnStartAsync(loaded.Context);
                Console.WriteLine($"  ✓ Started: {loaded.Manifest.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Failed to start {loaded.Manifest.Name}: {ex.Message}");
                Console.ResetColor();
                throw;
            }
        }
    }

    /// <summary>
    /// Phase 4: Stop all libraries (reverse dependency order).
    /// Libraries that depend on others are stopped first.
    /// </summary>
    public async Task StopAllAsync()
    {
        // Stop in reverse dependency order
        var orderedLibraries = _config.Libraries.EnableDependencyResolution
            ? ResolveDependencyOrder()
            : _libraries.ToList();

        orderedLibraries.Reverse();

        foreach (var loaded in orderedLibraries)
        {
            try
            {
                await loaded.Instance.OnStopAsync();
                Console.WriteLine($"  ✓ Stopped: {loaded.Manifest.Name}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"  ✗ Error stopping {loaded.Manifest.Name}: {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    /// <summary>
    /// Gets a library by type.
    /// </summary>
    public T? GetLibrary<T>() where T : class, ILibrary
    {
        return _libraries
            .Select(l => l.Instance as T)
            .FirstOrDefault(l => l != null);
    }

    /// <summary>
    /// Gets a library by ID.
    /// </summary>
    public ILibrary? GetLibrary(string libraryId)
    {
        _librariesById.TryGetValue(libraryId, out var library);
        return library;
    }

    /// <summary>
    /// Gets all loaded libraries.
    /// </summary>
    public IEnumerable<ILibrary> GetAllLibraries()
    {
        return _libraries.Select(l => l.Instance);
    }

    private LibraryContext CreateLibraryContext(string libraryId)
    {
        var libraryDir = _options.GetLibraryPath(libraryId);
        var logsDir = Path.Combine(libraryDir, "logs");

        var logger = new Logger(
            componentName: libraryId.ToUpper(),
            componentLogsPath: logsDir,
            minimumLevel: _config.Logging.GetGlobalLogLevel(),
            options: new LoggingOptions
            {
                GlobalLevel = _config.Logging.GetGlobalLogLevel(),
                EnableDebugMode = _config.Logging.EnableDebugMode,
                CentralizedDebugLog = _config.Logging.CentralizedDebugLog,
                FileNamePattern = _config.Logging.FileNamePattern,
                TimestampFormat = _config.Logging.TimestampFormat,
                EnableConsoleOutput = _config.Logging.EnableConsoleOutput,
                ConsoleMinimumLevel = _config.Logging.GetConsoleLogLevel()
            }
        );

        var configManager = new ConfigurationManager(libraryDir);
        var localizationManager = new LocalizationManager(
            Path.Combine(libraryDir, "localization"),
            _config.Localization.DefaultCulture
        );

        return new LibraryContext
        {
            LibraryId = libraryId,
            LibraryDirectory = libraryDir,
            Logger = logger,
            Configuration = configManager,
            Localization = localizationManager
        };
    }

    private Type? FindLibraryType(Assembly assembly)
    {
        return assembly.GetTypes()
            .FirstOrDefault(t => typeof(ILibrary).IsAssignableFrom(t) &&
                               !t.IsInterface &&
                               !t.IsAbstract);
    }

    private List<LoadedLibrary> ResolveDependencyOrder()
    {
        // Topological sort for dependency resolution
        var sorted = new List<LoadedLibrary>();
        var visited = new HashSet<string>();
        var visiting = new HashSet<string>();

        foreach (var library in _libraries)
        {
            Visit(library, sorted, visited, visiting);
        }

        return sorted;
    }

    private void Visit(LoadedLibrary library, List<LoadedLibrary> sorted, HashSet<string> visited, HashSet<string> visiting)
    {
        if (visited.Contains(library.Manifest.Id))
            return;

        if (visiting.Contains(library.Manifest.Id))
            throw new InvalidOperationException($"Circular dependency detected: {library.Manifest.Id}");

        visiting.Add(library.Manifest.Id);

        foreach (var dep in library.Manifest.Dependencies)
        {
            var dependency = _libraries.FirstOrDefault(l => l.Manifest.Id == dep.Id);

            // Optional dependencies that aren't found are skipped
            if (dependency == null && dep.IsOptional)
                continue;

            if (dependency != null)
            {
                Visit(dependency, sorted, visited, visiting);
            }
        }

        visiting.Remove(library.Manifest.Id);
        visited.Add(library.Manifest.Id);
        sorted.Add(library);
    }

    /// <summary>
    /// Validates all library dependencies including version requirements.
    /// Throws InvalidOperationException if validation fails.
    /// </summary>
    private void ValidateDependencies(IEnumerable<LoadedLibrary> libraries)
    {
        var errors = new List<string>();

        foreach (var library in libraries)
        {
            foreach (var dependency in library.Instance.Manifest.Dependencies)
            {
                // Find the dependency
                var depLibrary = _libraries.FirstOrDefault(l => l.Instance.Manifest.Id == dependency.Id);

                // Check if dependency exists
                if (depLibrary == null)
                {
                    if (!dependency.IsOptional)
                    {
                        errors.Add(
                            $"Library '{library.Instance.Manifest.Name}' requires missing dependency: '{dependency.Id}'" +
                            (dependency.MinVersion != null ? $" (>= {dependency.MinVersion})" : "")
                        );
                    }
                    continue; // Skip version check if not found
                }

                // Check version requirement
                if (!string.IsNullOrEmpty(dependency.MinVersion))
                {
                    if (!Utilities.SemanticVersion.TryParse(depLibrary.Instance.Manifest.Version, out var installedVersion) || installedVersion is null)
                    {
                        errors.Add(
                            $"Library '{depLibrary.Instance.Manifest.Name}' has invalid version format: '{depLibrary.Instance.Manifest.Version}'"
                        );
                        continue;
                    }

                    if (!Utilities.SemanticVersion.TryParse(dependency.MinVersion, out var requiredVersion) || requiredVersion is null)
                    {
                        errors.Add(
                            $"Library '{library.Instance.Manifest.Name}' specifies invalid minimum version for '{dependency.Id}': '{dependency.MinVersion}'"
                        );
                        continue;
                    }

                    if (installedVersion < requiredVersion)
                    {
                        errors.Add(
                            $"Library '{library.Instance.Manifest.Name}' requires '{dependency.Id}' version >= {dependency.MinVersion}, " +
                            $"but version {installedVersion} is loaded"
                        );
                    }
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                $"Dependency validation failed:\n  " + string.Join("\n  ", errors)
            );
        }
    }

    private Assembly? OnAssemblyResolve(object? sender, ResolveEventArgs args)
    {
        var assemblyName = new AssemblyName(args.Name);

        // Try to load from each library directory
        foreach (var library in _libraries)
        {
            var libraryDir = Path.GetDirectoryName(library.AssemblyPath);
            if (libraryDir == null) continue;

            var assemblyPath = Path.Combine(libraryDir, assemblyName.Name + ".dll");
            if (File.Exists(assemblyPath))
            {
                return Assembly.LoadFrom(assemblyPath);
            }
        }

        return null;
    }
}

/// <summary>
/// Information about a loaded library.
/// </summary>
public class LoadedLibrary
{
    /// <summary>
    /// The loaded library instance.
    /// </summary>
    public required ILibrary Instance { get; init; }

    /// <summary>
    /// Manifest metadata for the library.
    /// </summary>
    public required LibraryManifest Manifest { get; init; }

    /// <summary>
    /// Assembly path used to load the library.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    /// Timestamp when the library was loaded.
    /// </summary>
    public DateTime LoadedAt { get; init; }

    /// <summary>
    /// Library context, populated after configuration.
    /// </summary>
    public LibraryContext? Context { get; set; }
}
