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
    public required string Id { get; set; }
    public required string Name { get; set; }
    public required string Version { get; set; }
    public string? Description { get; set; }
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
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Version { get; init; }
    public string? Description { get; init; }
    public string? Author { get; init; }
    public required string AssemblyPath { get; init; }
    public required IPlugin Instance { get; init; }
    public required PluginLoadContext LoadContext { get; init; }
    public WeakReference? WeakReference { get; set; }
    public DateTime LoadedAt { get; init; }
}

/// <summary>
/// Custom AssemblyLoadContext for plugin isolation.
/// Allows plugins to be unloaded and garbage collected.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(name: pluginPath, isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);
        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        return null;
    }

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
