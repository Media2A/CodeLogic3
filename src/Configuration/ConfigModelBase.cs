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
    /// <summary>
    /// Configuration section name used in config files.
    /// </summary>
    public string SectionName { get; }

    /// <summary>
    /// Configuration schema version.
    /// </summary>
    public int Version { get; set; } = 1;

    /// <summary>
    /// Creates a new configuration section attribute.
    /// </summary>
    /// <param name="sectionName">Name of the configuration section.</param>
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
    /// <summary>
    /// Indicates whether the configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Validation error messages when invalid.
    /// </summary>
    public List<string> Errors { get; set; } = new();

    /// <summary>
    /// Creates a valid result with no errors.
    /// </summary>
    public static ConfigValidationResult Valid()
    {
        return new ConfigValidationResult { IsValid = true };
    }

    /// <summary>
    /// Creates an invalid result with a set of errors.
    /// </summary>
    /// <param name="errors">Validation errors.</param>
    public static ConfigValidationResult Invalid(List<string> errors)
    {
        return new ConfigValidationResult { IsValid = false, Errors = errors };
    }

    /// <summary>
    /// Creates an invalid result with a single error.
    /// </summary>
    /// <param name="error">Validation error.</param>
    public static ConfigValidationResult Invalid(string error)
    {
        return new ConfigValidationResult { IsValid = false, Errors = new List<string> { error } };
    }
}
