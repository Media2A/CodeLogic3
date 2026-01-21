namespace CodeLogic.Models;

/// <summary>
/// Options for configuring CodeLogic framework paths and behavior.
/// </summary>
public class CodeLogicOptions
{
    /// <summary>
    /// Root directory for all CodeLogic files.
    /// Default: "CodeLogic"
    /// Set to null to use app base directory directly.
    /// </summary>
    public string? RootDirectory { get; set; } = "CodeLogic";

    /// <summary>
    /// Directory name for CL.* libraries (relative to RootDirectory).
    /// Libraries will be in: {RootDirectory}/CL.{LibraryName}/
    /// Default: null (libraries directly in RootDirectory)
    /// </summary>
    public string? LibrariesSubDirectory { get; set; } = null;

    /// <summary>
    /// Directory for external plugins (relative to app base).
    /// Default: "Plugins"
    /// </summary>
    public string PluginsDirectory { get; set; } = "Plugins";

    /// <summary>
    /// Gets the full path to the CodeLogic root directory.
    /// </summary>
    public string GetRootPath()
    {
        if (RootDirectory == null)
            return AppDomain.CurrentDomain.BaseDirectory;

        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, RootDirectory);
    }

    /// <summary>
    /// Gets the full path to a library's directory.
    /// Example: {AppBase}/CodeLogic/CL.MySQL/
    /// </summary>
    public string GetLibraryPath(string libraryName)
    {
        var rootPath = GetRootPath();

        if (LibrariesSubDirectory != null)
        {
            return Path.Combine(rootPath, LibrariesSubDirectory, $"CL.{libraryName}");
        }

        return Path.Combine(rootPath, $"CL.{libraryName}");
    }

    /// <summary>
    /// Gets the full path to the plugins directory.
    /// </summary>
    public string GetPluginsPath()
    {
        return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PluginsDirectory);
    }

    /// <summary>
    /// Gets the full path to the framework directory (for centralized logs).
    /// </summary>
    public string GetFrameworkPath()
    {
        return Path.Combine(GetRootPath(), "Framework");
    }

    /// <summary>
    /// Gets the CodeLogic.json configuration file path.
    /// </summary>
    public string GetCodeLogicConfigPath()
    {
        return Path.Combine(GetRootPath(), "CodeLogic.json");
    }
}
