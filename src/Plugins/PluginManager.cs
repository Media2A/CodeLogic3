using System.Reflection;
using CodeLogic.Abstractions;
using CodeLogic.Logging;

namespace CodeLogic.Plugins;

/// <summary>
/// Manages hot-pluggable external plugins.
/// Separate from CodeLogic framework - application-specific plugin system.
/// Supports dynamic load/unload/reload at runtime.
/// </summary>
public class PluginManager
{
    private readonly PluginOptions _options;
    private readonly Dictionary<string, LoadedPluginInfo> _plugins = new();
    private readonly FileSystemWatcher? _fileWatcher;

    /// <summary>
    /// Raised when a plugin is successfully loaded.
    /// </summary>
    public event Action<string>? OnPluginLoaded;

    /// <summary>
    /// Raised when a plugin is unloaded.
    /// </summary>
    public event Action<string>? OnPluginUnloaded;

    /// <summary>
    /// Raised when a plugin load/unload operation fails.
    /// </summary>
    public event Action<string, Exception>? OnPluginError;

    /// <summary>
    /// Creates a plugin manager with optional configuration.
    /// </summary>
    /// <param name="options">Plugin manager options, or null to use defaults.</param>
    public PluginManager(PluginOptions? options = null)
    {
        _options = options ?? new PluginOptions();
        Directory.CreateDirectory(_options.PluginsDirectory);

        // Setup file watcher for auto-reload
        if (_options.WatchForChanges)
        {
            _fileWatcher = new FileSystemWatcher(_options.PluginsDirectory, "*.dll")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName,
                EnableRaisingEvents = true
            };

            _fileWatcher.Changed += async (s, e) => await OnFileChangedAsync(e.FullPath);
        }
    }

    /// <summary>
    /// Discovers all plugins in the plugins directory.
    /// </summary>
    public async Task<List<string>> DiscoverPluginsAsync()
    {
        var pluginPaths = new List<string>();

        if (!Directory.Exists(_options.PluginsDirectory))
            return pluginPaths;

        // Find all *.Plugin directories
        var directories = Directory.GetDirectories(_options.PluginsDirectory, "*.Plugin", SearchOption.TopDirectoryOnly);

        foreach (var dir in directories)
        {
            var dirName = Path.GetFileName(dir);
            var dllPath = Path.Combine(dir, $"{dirName}.dll");

            if (File.Exists(dllPath))
            {
                pluginPaths.Add(dllPath);
            }
        }

        return pluginPaths;
    }

    /// <summary>
    /// Loads a plugin from the specified path.
    /// </summary>
    public async Task LoadPluginAsync(string pluginPath)
    {
        if (!File.Exists(pluginPath))
        {
            throw new FileNotFoundException($"Plugin file not found: {pluginPath}");
        }

        try
        {
            // Create isolated load context
            var loadContext = new PluginLoadContext(pluginPath);

            // Load plugin assembly
            var assembly = loadContext.LoadFromAssemblyPath(pluginPath);

            // Find IPlugin implementation
            var pluginType = assembly.GetTypes()
                .FirstOrDefault(t => typeof(IPlugin).IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract);

            if (pluginType == null)
            {
                throw new InvalidOperationException($"No IPlugin implementation found in {Path.GetFileName(pluginPath)}");
            }

            // Create plugin instance
            var plugin = (IPlugin)Activator.CreateInstance(pluginType)!;

            // Create plugin context
            var context = CreatePluginContext(plugin.Id, pluginPath);

            // Initialize plugin
            await plugin.OnLoadAsync(context);

            // Store loaded plugin
            var pluginInfo = new LoadedPluginInfo
            {
                Id = plugin.Id,
                Name = plugin.Name,
                Version = plugin.Version,
                Description = plugin.Description,
                Author = plugin.Author,
                AssemblyPath = pluginPath,
                Instance = plugin,
                LoadContext = loadContext,
                WeakReference = new WeakReference(loadContext),
                LoadedAt = DateTime.UtcNow
            };

            _plugins[plugin.Id] = pluginInfo;

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"  ✓ Loaded plugin: {plugin.Name} v{plugin.Version}");
            Console.ResetColor();

            OnPluginLoaded?.Invoke(plugin.Id);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Failed to load plugin from {Path.GetFileName(pluginPath)}: {ex.Message}");
            Console.ResetColor();

            OnPluginError?.Invoke(pluginPath, ex);
            throw;
        }
    }

    /// <summary>
    /// Unloads a plugin by ID.
    /// </summary>
    public async Task UnloadPluginAsync(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            Console.WriteLine($"  ⚠ Plugin '{pluginId}' not loaded");
            return;
        }

        try
        {
            // Call plugin cleanup
            await pluginInfo.Instance.OnUnloadAsync();
            pluginInfo.Instance.Dispose();

            // Unload assembly context
            pluginInfo.LoadContext.Unload();

            // Remove from dictionary
            _plugins.Remove(pluginId);

            // Force garbage collection to actually unload
            for (int i = 0; pluginInfo.WeakReference != null && pluginInfo.WeakReference.IsAlive && i < 10; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(10);
            }

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"  ✓ Unloaded plugin: {pluginInfo.Name}");
            Console.ResetColor();

            OnPluginUnloaded?.Invoke(pluginId);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Error unloading plugin '{pluginId}': {ex.Message}");
            Console.ResetColor();

            OnPluginError?.Invoke(pluginId, ex);
        }
    }

    /// <summary>
    /// Reloads a plugin (unload + load).
    /// </summary>
    public async Task ReloadPluginAsync(string pluginId)
    {
        if (!_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            throw new InvalidOperationException($"Plugin '{pluginId}' not loaded");
        }

        var pluginPath = pluginInfo.AssemblyPath;

        Console.WriteLine($"  → Reloading plugin: {pluginInfo.Name}...");

        await UnloadPluginAsync(pluginId);
        await Task.Delay(200); // Small delay for file system
        await LoadPluginAsync(pluginPath);

        Console.WriteLine($"  ✓ Reloaded: {pluginInfo.Name}");
    }

    /// <summary>
    /// Loads all discovered plugins.
    /// </summary>
    public async Task LoadAllAsync()
    {
        var pluginPaths = await DiscoverPluginsAsync();

        Console.WriteLine($"\n→ Loading {pluginPaths.Count} plugin(s)...");

        foreach (var path in pluginPaths)
        {
            try
            {
                await LoadPluginAsync(path);
            }
            catch
            {
                // Error already logged in LoadPluginAsync
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Unloads all plugins.
    /// </summary>
    public async Task UnloadAllAsync()
    {
        var pluginIds = _plugins.Keys.ToList();

        Console.WriteLine($"\n→ Unloading {pluginIds.Count} plugin(s)...");

        foreach (var pluginId in pluginIds)
        {
            await UnloadPluginAsync(pluginId);
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Gets a plugin by ID and type.
    /// </summary>
    public T? GetPlugin<T>(string pluginId) where T : class, IPlugin
    {
        if (_plugins.TryGetValue(pluginId, out var pluginInfo))
        {
            return pluginInfo.Instance as T;
        }

        return null;
    }

    /// <summary>
    /// Gets all loaded plugins.
    /// </summary>
    public IEnumerable<IPlugin> GetAllPlugins()
    {
        return _plugins.Values.Select(p => p.Instance);
    }

    /// <summary>
    /// Gets plugin information.
    /// </summary>
    public IEnumerable<LoadedPluginInfo> GetPluginInfos()
    {
        return _plugins.Values;
    }

    private PluginContext CreatePluginContext(string pluginId, string pluginPath)
    {
        var pluginDir = Path.GetDirectoryName(pluginPath)!;
        var logsDir = Path.Combine(pluginDir, "logs");

        var logger = new Logger(
            componentName: pluginId,
            componentLogsPath: logsDir,
            minimumLevel: LogLevel.Info,
            options: new LoggingOptions
            {
                GlobalLevel = LogLevel.Info,
                EnableDebugMode = false,
                CentralizedDebugLog = false,
                EnableConsoleOutput = true,
                ConsoleMinimumLevel = LogLevel.Info
            }
        );

        return new PluginContext
        {
            PluginId = pluginId,
            PluginDirectory = pluginDir,
            Logger = logger
        };
    }

    private async Task OnFileChangedAsync(string filePath)
    {
        if (!_options.EnableHotReload)
            return;

        // Find plugin by assembly path
        var plugin = _plugins.Values.FirstOrDefault(p => p.AssemblyPath == filePath);
        if (plugin == null)
            return;

        Console.WriteLine($"\n  🔄 File change detected: {Path.GetFileName(filePath)}");

        try
        {
            await ReloadPluginAsync(plugin.Id);
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  ✗ Auto-reload failed: {ex.Message}");
            Console.ResetColor();
        }
    }

    /// <summary>
    /// Disposes the plugin manager and unloads plugins.
    /// </summary>
    public void Dispose()
    {
        _fileWatcher?.Dispose();

        // Unload all plugins synchronously
        var task = UnloadAllAsync();
        task.Wait();
    }
}

/// <summary>
/// Options for PluginManager.
/// </summary>
public class PluginOptions
{
    /// <summary>
    /// Base directory where plugin folders live.
    /// </summary>
    public string PluginsDirectory { get; set; } = "Plugins";

    /// <summary>
    /// Enables hot reload behavior when plugins change on disk.
    /// </summary>
    public bool EnableHotReload { get; set; } = true;

    /// <summary>
    /// Enables filesystem watching for plugin changes.
    /// </summary>
    public bool WatchForChanges { get; set; } = false;
}
