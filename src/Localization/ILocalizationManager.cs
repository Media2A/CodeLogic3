namespace CodeLogic.Localization;

/// <summary>
/// Localization manager interface for libraries and plugins.
/// </summary>
public interface ILocalizationManager
{
    /// <summary>
    /// Registers a localization model type.
    /// Model will be auto-generated for all supported cultures if files don't exist.
    /// </summary>
    void Register<T>() where T : LocalizationModelBase, new();

    /// <summary>
    /// Gets a loaded localization model for the specified culture.
    /// </summary>
    T Get<T>(string? culture = null) where T : LocalizationModelBase, new();

    /// <summary>
    /// Generates default localization files for all supported cultures.
    /// </summary>
    Task GenerateTemplatesAsync<T>(List<string> cultures) where T : LocalizationModelBase, new();

    /// <summary>
    /// Loads localization for all supported cultures.
    /// </summary>
    Task LoadAsync<T>(List<string> cultures) where T : LocalizationModelBase, new();

    /// <summary>
    /// Generates all registered localizations for all cultures.
    /// </summary>
    Task GenerateAllTemplatesAsync(List<string> cultures);

    /// <summary>
    /// Loads all registered localizations for all cultures.
    /// </summary>
    Task LoadAllAsync(List<string> cultures);
}
