using System.Reflection;
using System.Runtime.Loader;
using CodeLogic.Logging;

namespace CodeLogic.Abstractions;

/// <summary>
/// Interface for external application plugins.
/// These are user-created plugins for extending applications.
/// Hot-pluggable - can be loaded/unloaded dynamically at runtime.
/// </summary>
public interface IPlugin : IDisposable
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Plugin display name.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Plugin version.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Plugin description.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Plugin author.
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Whether the plugin is currently loaded and active.
    /// </summary>
    bool IsLoaded { get; }

    /// <summary>
    /// Called when plugin is loaded.
    /// </summary>
    Task OnLoadAsync(PluginContext context);

    /// <summary>
    /// Called when plugin is unloaded.
    /// </summary>
    Task OnUnloadAsync();

    /// <summary>
    /// Health check for monitoring.
    /// </summary>
    Task<HealthStatus> HealthCheckAsync();
}

/// <summary>
/// Attribute for defining plugin metadata.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class PluginManifestAttribute : Attribute
{
    /// <summary>
    /// Unique plugin identifier.
    /// </summary>
    public required string Id { get; set; }

    /// <summary>
    /// Plugin display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Plugin version string.
    /// </summary>
    public required string Version { get; set; }

    /// <summary>
    /// Optional plugin description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Optional plugin author.
    /// </summary>
    public string? Author { get; set; }
}

/// <summary>
/// Context provided to each plugin containing paths and services.
/// Each plugin gets isolated directory in Plugins/{PluginId}.Plugin/
/// </summary>
public class PluginContext
{
    /// <summary>
    /// Plugin ID
    /// </summary>
    public required string PluginId { get; init; }

    /// <summary>
    /// Root directory for this plugin
    /// Example: Plugins/DarkTheme.Plugin/
    /// </summary>
    public required string PluginDirectory { get; init; }

    /// <summary>
    /// Config file path for this plugin
    /// Example: Plugins/DarkTheme.Plugin/config.json
    /// </summary>
    public string ConfigPath => Path.Combine(PluginDirectory, "config.json");

    /// <summary>
    /// Data directory for this plugin
    /// Example: Plugins/DarkTheme.Plugin/data/
    /// </summary>
    public string DataDirectory => Path.Combine(PluginDirectory, "data");

    /// <summary>
    /// Logs directory for this plugin
    /// Example: Plugins/DarkTheme.Plugin/logs/
    /// </summary>
    public string LogsDirectory => Path.Combine(PluginDirectory, "logs");

    /// <summary>
    /// Logger instance for this plugin
    /// </summary>
    public required ILogger Logger { get; init; }
}

/// <summary>
/// Information about a loaded plugin.
/// </summary>
public class LoadedPluginInfo
{
    /// <summary>
    /// Plugin identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Plugin display name.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Plugin version string.
    /// </summary>
    public required string Version { get; init; }

    /// <summary>
    /// Optional plugin description.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Optional plugin author.
    /// </summary>
    public string? Author { get; init; }

    /// <summary>
    /// Assembly path the plugin was loaded from.
    /// </summary>
    public required string AssemblyPath { get; init; }

    /// <summary>
    /// Plugin instance.
    /// </summary>
    public required IPlugin Instance { get; init; }

    /// <summary>
    /// Load context used for plugin isolation and unloading.
    /// </summary>
    public required PluginLoadContext LoadContext { get; init; }

    /// <summary>
    /// Weak reference used to determine unloadability.
    /// </summary>
    public WeakReference? WeakReference { get; set; }

    /// <summary>
    /// Timestamp when the plugin was loaded.
    /// </summary>
    public DateTime LoadedAt { get; init; }
}

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// Allows plugins to be unloaded and garbage collected.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    /// <summary>
    /// Creates a collectible load context rooted at the plugin path.
    /// </summary>
    /// <param name="pluginPath">Path to the plugin assembly.</param>
    public PluginLoadContext(string pluginPath) : base(name: pluginPath, isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    /// <summary>
    /// Loads an assembly by name from the plugin's dependency resolver.
    /// </summary>
    /// <param name="assemblyName">The name of the assembly to load.</param>
    /// <returns>The loaded assembly, or null if not found.</returns>
    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

    /// <summary>
    /// Loads an unmanaged DLL by name from the plugin's dependency resolver.
    /// </summary>
    /// <param name="unmanagedDllName">The name of the unmanaged DLL to load.</param>
    /// <returns>A pointer to the loaded DLL, or IntPtr.Zero if not found.</returns>
    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);
        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
