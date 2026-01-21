namespace CodeLogic.Localization;

/// <summary>
/// Base class for localization models.
/// </summary>
public abstract class LocalizationModelBase
{
    /// <summary>
    /// Culture code for this localization (e.g., "en-US", "de-DE")
    /// </summary>
    public string Culture { get; set; } = "en-US";
}

/// <summary>
/// Attribute for marking localization section.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class LocalizationSectionAttribute : Attribute
{
    /// <summary>
    /// Localization section name used for grouping strings.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Creates a new section attribute.
    /// </summary>
    /// <param name="sectionName">Name of the localization section.</param>
    public LocalizationSectionAttribute(string sectionName)
    {
        SectionName = sectionName;
    }
}

/// <summary>
/// Attribute for marking localized strings.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class LocalizedStringAttribute : Attribute
{
    /// <summary>
    /// Optional description to include in localization templates.
    /// </summary>
    public string? Description { get; set; }
}
