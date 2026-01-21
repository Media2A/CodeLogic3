namespace CodeLogic.Utilities;

/// <summary>
/// Simple semantic version parser and comparer.
/// Supports: major.minor.patch (e.g., "2.1.5")
/// </summary>
public class SemanticVersion : IComparable<SemanticVersion>
{
    /// <summary>
    /// Major version component.
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Minor version component.
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Patch version component.
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Creates a semantic version from its components.
    /// </summary>
    /// <param name="major">Major version component.</param>
    /// <param name="minor">Minor version component.</param>
    /// <param name="patch">Patch version component.</param>
    public SemanticVersion(int major, int minor, int patch)
    {
        if (major < 0 || minor < 0 || patch < 0)
            throw new ArgumentException("Version components cannot be negative");

        Major = major;
        Minor = minor;
        Patch = patch;
    }

    /// <summary>
    /// Parses a semantic version string (e.g., "2.1.5")
    /// </summary>
    public static SemanticVersion Parse(string version)
    {
        if (string.IsNullOrWhiteSpace(version))
            throw new ArgumentException("Version string cannot be null or empty", nameof(version));

        var parts = version.Trim().Split('.');

        if (parts.Length != 3)
            throw new FormatException($"Invalid semantic version format: '{version}'. Expected format: major.minor.patch");

        if (!int.TryParse(parts[0], out var major))
            throw new FormatException($"Invalid major version: '{parts[0]}'");

        if (!int.TryParse(parts[1], out var minor))
            throw new FormatException($"Invalid minor version: '{parts[1]}'");

        if (!int.TryParse(parts[2], out var patch))
            throw new FormatException($"Invalid patch version: '{parts[2]}'");

        return new SemanticVersion(major, minor, patch);
    }

    /// <summary>
    /// Tries to parse a semantic version string
    /// </summary>
    public static bool TryParse(string version, out SemanticVersion? result)
    {
        try
        {
            result = Parse(version);
            return true;
        }
        catch
        {
            result = null;
            return false;
        }
    }

    /// <summary>
    /// Compares this version to another version
    /// </summary>
    public int CompareTo(SemanticVersion? other)
    {
        if (other == null) return 1;

        if (Major != other.Major) return Major.CompareTo(other.Major);
        if (Minor != other.Minor) return Minor.CompareTo(other.Minor);
        return Patch.CompareTo(other.Patch);
    }

    /// <summary>
    /// Determines whether one version is greater than another.
    /// </summary>
    public static bool operator >(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) > 0;

    /// <summary>
    /// Determines whether one version is less than another.
    /// </summary>
    public static bool operator <(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) < 0;

    /// <summary>
    /// Determines whether one version is greater than or equal to another.
    /// </summary>
    public static bool operator >=(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) >= 0;

    /// <summary>
    /// Determines whether one version is less than or equal to another.
    /// </summary>
    public static bool operator <=(SemanticVersion a, SemanticVersion b)
        => a.CompareTo(b) <= 0;

    /// <summary>
    /// Determines whether two versions are equal.
    /// </summary>
    public static bool operator ==(SemanticVersion? a, SemanticVersion? b)
    {
        if (ReferenceEquals(a, b)) return true;
        if (a is null || b is null) return false;
        return a.CompareTo(b) == 0;
    }

    /// <summary>
    /// Determines whether two versions are not equal.
    /// </summary>
    public static bool operator !=(SemanticVersion? a, SemanticVersion? b)
        => !(a == b);

    /// <summary>
    /// Determines whether this instance equals another object.
    /// </summary>
    public override bool Equals(object? obj)
        => obj is SemanticVersion other && this == other;

    /// <summary>
    /// Returns a hash code for this version.
    /// </summary>
    public override int GetHashCode()
        => HashCode.Combine(Major, Minor, Patch);

    /// <summary>
    /// Returns the version in "major.minor.patch" format.
    /// </summary>
    public override string ToString() => $"{Major}.{Minor}.{Patch}";
}
