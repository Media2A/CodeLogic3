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
    public string SectionName { get; }

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
    public string? Description { get; set; }
}
