using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeLogic.Localization;

/// <summary>
/// Localization manager implementation for a single component (library or plugin).
/// Each component has localization files in its localization/ directory.
/// </summary>
public class LocalizationManager : ILocalizationManager
{
    private readonly string _localizationDirectory;
    private readonly string _defaultCulture;
    private readonly Dictionary<Type, Dictionary<string, object>> _localizations = new();
    private readonly Dictionary<Type, bool> _registered = new();

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Creates a localization manager scoped to a component directory.
    /// </summary>
    /// <param name="localizationDirectory">Directory containing localization files.</param>
    /// <param name="defaultCulture">Default culture to use when none is specified.</param>
    public LocalizationManager(string localizationDirectory, string defaultCulture = "en-US")
    {
        _localizationDirectory = localizationDirectory;
        _defaultCulture = defaultCulture;
        Directory.CreateDirectory(_localizationDirectory);
    }

    /// <summary>
    /// Registers a localization model type for template generation and loading.
    /// </summary>
    public void Register<T>() where T : LocalizationModelBase, new()
    {
        var type = typeof(T);
        _registered[type] = true;
    }

    /// <summary>
    /// Gets the localization instance for a culture, falling back to default if needed.
    /// </summary>
    /// <param name="culture">Culture code to retrieve, or null for default.</param>
    public T Get<T>(string? culture = null) where T : LocalizationModelBase, new()
    {
        culture ??= _defaultCulture;
        var type = typeof(T);

        if (_localizations.TryGetValue(type, out var cultures) &&
            cultures.TryGetValue(culture, out var localization))
        {
            return (T)localization;
        }

        // Fallback to default culture
        if (culture != _defaultCulture && cultures != null &&
            cultures.TryGetValue(_defaultCulture, out var defaultLocalization))
        {
            return (T)defaultLocalization;
        }

        throw new InvalidOperationException(
            $"Localization type {type.Name} for culture '{culture}' not loaded. Call LoadAsync first.");
    }

    /// <summary>
    /// Generates localization template files for the given cultures.
    /// </summary>
    /// <param name="cultures">List of culture codes to generate templates for.</param>
    public async Task GenerateTemplatesAsync<T>(List<string> cultures) where T : LocalizationModelBase, new()
    {
        foreach (var culture in cultures)
        {
            var filePath = GetLocalizationFilePath<T>(culture);

            if (File.Exists(filePath))
                continue;

            var template = new T { Culture = culture };
            var json = JsonSerializer.Serialize(template, _jsonOptions);
            await File.WriteAllTextAsync(filePath, json);
        }
    }

    /// <summary>
    /// Loads localization files for the given cultures into memory.
    /// </summary>
    /// <param name="cultures">List of culture codes to load.</param>
    public async Task LoadAsync<T>(List<string> cultures) where T : LocalizationModelBase, new()
    {
        var type = typeof(T);

        if (!_localizations.ContainsKey(type))
        {
            _localizations[type] = new Dictionary<string, object>();
        }

        foreach (var culture in cultures)
        {
            var filePath = GetLocalizationFilePath<T>(culture);

            if (!File.Exists(filePath))
            {
                await GenerateTemplatesAsync<T>(new List<string> { culture });
            }

            var json = await File.ReadAllTextAsync(filePath);
            var localization = JsonSerializer.Deserialize<T>(json, _jsonOptions);

            if (localization == null)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize localization from {filePath}");
            }

            localization.Culture = culture;
            _localizations[type][culture] = localization;
        }
    }

    private string GetLocalizationFilePath<T>(string culture) where T : LocalizationModelBase, new()
    {
        // Example: CodeLogic/CL.MySQL/localization/en-US.json
        return Path.Combine(_localizationDirectory, $"{culture}.json");
    }

    /// <summary>
    /// Generates all registered localizations for all cultures.
    /// </summary>
    public async Task GenerateAllTemplatesAsync(List<string> cultures)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(LocalizationManager)
                .GetMethod(nameof(GenerateTemplatesAsync))!
                .MakeGenericMethod(type);

            var task = (Task)method.Invoke(this, new object[] { cultures })!;
            await task;
        }
    }

    /// <summary>
    /// Loads all registered localizations for all cultures.
    /// </summary>
    public async Task LoadAllAsync(List<string> cultures)
    {
        foreach (var type in _registered.Keys)
        {
            var method = typeof(LocalizationManager)
                .GetMethod(nameof(LoadAsync))!
                .MakeGenericMethod(type);

            var task = (Task)method.Invoke(this, new object[] { cultures })!;
            await task;
        }
    }
}
