using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeLogic.Configuration;

/// <summary>
/// Configuration manager implementation for a single component (library or plugin).
/// Supports multiple configuration levels:
/// - config.json (main)
/// - config.database.json
/// - config.connection.json
/// etc.
/// </summary>
public class ConfigurationManager : IConfigurationManager
{
    private readonly string _baseDirectory;
    private readonly Dictionary<Type, object> _configurations = new();
    private readonly Dictionary<Type, string> _registered = new(); // Type -> config file name (e.g., "database")

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public ConfigurationManager(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
    }

    /// <summary>
    /// Registers a configuration type with optional sub-config name.
    /// Examples:
    /// - Register&lt;MainConfig&gt;() -> config.json
    /// - Register&lt;DatabaseConfig&gt;("database") -> config.database.json
    /// - Register&lt;ConnectionConfig&gt;("connection") -> config.connection.json
    /// </summary>
    public void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new()
    {
        var type = typeof(T);
        _registered[type] = subConfigName ?? string.Empty;
    }

    public T Get<T>() where T : ConfigModelBase, new()
    {
        var type = typeof(T);

        if (_configurations.TryGetValue(type, out var config))
        {
            return (T)config;
        }

        throw new InvalidOperationException($"Configuration type {type.Name} not loaded. Call LoadAsync first.");
    }

    private string GetConfigFilePath<T>() where T : ConfigModelBase, new()
    {
        var type = typeof(T);

        if (!_registered.TryGetValue(type, out var subConfigName))
        {
            throw new InvalidOperationException($"Configuration type {type.Name} not registered.");
        }

        // Build file name: config.json or config.database.json
        var fileName = string.IsNullOrEmpty(subConfigName)
            ? "config.json"
            : $"config.{subConfigName}.json";

        return Path.Combine(_baseDirectory, fileName);
    }

    public async Task GenerateDefaultAsync<T>() where T : ConfigModelBase, new()
    {
        var configPath = GetConfigFilePath<T>();

        if (File.Exists(configPath))
            return;

        var defaultConfig = new T();
        await SaveAsync(defaultConfig);
    }

    public async Task LoadAsync<T>() where T : ConfigModelBase, new()
    {
        var type = typeof(T);
        var configPath = GetConfigFilePath<T>();

        if (!File.Exists(configPath))
        {
            // Generate default
            await GenerateDefaultAsync<T>();
        }

        var json = await File.ReadAllTextAsync(configPath);
        var config = JsonSerializer.Deserialize<T>(json, _jsonOptions);

        if (config == null)
        {
            throw new InvalidOperationException($"Failed to deserialize configuration from {configPath}");
        }

        // Validate
        var validation = config.Validate();
        if (!validation.IsValid)
        {
            throw new InvalidOperationException(
                $"Configuration validation failed: {string.Join(", ", validation.Errors)}");
        }

        _configurations[type] = config;
    }

    public async Task SaveAsync<T>(T config) where T : ConfigModelBase, new()
    {
        var configPath = GetConfigFilePath<T>();
        var directory = Path.GetDirectoryName(configPath);

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var json = JsonSerializer.Serialize(config, _jsonOptions);
        await File.WriteAllTextAsync(configPath, json);
    }

    /// <summary>
    /// Generates all registered configurations if they don't exist.
    /// </summary>
    public async Task GenerateAllDefaultsAsync()
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(GenerateDefaultAsync))!
                .MakeGenericMethod(type);

            var task = (Task)method.Invoke(this, null)!;
            await task;
        }
    }

    /// <summary>
    /// Loads all registered configurations.
    /// </summary>
    public async Task LoadAllAsync()
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(ConfigurationManager)
                .GetMethod(nameof(LoadAsync))!
                .MakeGenericMethod(type);

            var task = (Task)method.Invoke(this, null)!;
            await task;
        }
    }
}
