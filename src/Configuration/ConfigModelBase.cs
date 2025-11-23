using System.ComponentModel.DataAnnotations;

namespace CodeLogic.Configuration;

/// <summary>
/// Base class for configuration models.
/// </summary>
public abstract class ConfigModelBase
{
    /// <summary>
    /// Validates the configuration model.
    /// Override to add custom validation logic.
    /// </summary>
    public virtual ConfigValidationResult Validate()
    {
        var context = new ValidationContext(this);
        var results = new List<System.ComponentModel.DataAnnotations.ValidationResult>();

        if (Validator.TryValidateObject(this, context, results, validateAllProperties: true))
        {
            return ConfigValidationResult.Valid();
        }

        var errors = results.Select(r => r.ErrorMessage ?? "Unknown error").ToList();
        return ConfigValidationResult.Invalid(errors);
    }
}

/// <summary>
/// Attribute for marking configuration section.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class ConfigSectionAttribute : Attribute
{
    public string SectionName { get; }
    public int Version { get; set; } = 1;

    public ConfigSectionAttribute(string sectionName)
    {
        SectionName = sectionName;
    }
}

/// <summary>
/// Configuration validation result.
/// </summary>
public class ConfigValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ConfigValidationResult Valid()
    {
        return new ConfigValidationResult { IsValid = true };
    }

    public static ConfigValidationResult Invalid(List<string> errors)
    {
        return new ConfigValidationResult { IsValid = false, Errors = errors };
    }

    public static ConfigValidationResult Invalid(string error)
    {
        return new ConfigValidationResult { IsValid = false, Errors = new List<string> { error } };
    }
}
