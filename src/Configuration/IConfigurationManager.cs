namespace CodeLogic.Configuration;

/// <summary>
/// Configuration manager interface for libraries and plugins.
/// Supports multi-level configurations (config.json, config.database.json, etc.)
/// </summary>
public interface IConfigurationManager
{
    /// <summary>
    /// Registers a configuration model type with optional sub-config name.
    /// Examples:
    /// - Register&lt;MainConfig&gt;() -> config.json
    /// - Register&lt;DatabaseConfig&gt;("database") -> config.database.json
    /// </summary>
    void Register<T>(string? subConfigName = null) where T : ConfigModelBase, new();

    /// <summary>
    /// Gets a loaded configuration model.
    /// </summary>
    T Get<T>() where T : ConfigModelBase, new();

    /// <summary>
    /// Generates default config file if it doesn't exist.
    /// </summary>
    Task GenerateDefaultAsync<T>() where T : ConfigModelBase, new();

    /// <summary>
    /// Loads configuration from file.
    /// </summary>
    Task LoadAsync<T>() where T : ConfigModelBase, new();

    /// <summary>
    /// Saves configuration to file.
    /// </summary>
    Task SaveAsync<T>(T config) where T : ConfigModelBase, new();

    /// <summary>
    /// Generates all registered configurations if they don't exist.
    /// </summary>
    Task GenerateAllDefaultsAsync();

    /// <summary>
    /// Loads all registered configurations.
    /// </summary>
    Task LoadAllAsync();
}
