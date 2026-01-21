# CodeLogic 3.0 - Build & Configuration Guide

**Version:** 3.0.0
**Last Updated:** November 2025

---

## 📋 Table of Contents

- [Building the Framework](#building-the-framework)
- [Project Structure](#project-structure)
- [Centralized Configuration System](#centralized-configuration-system)
- [Centralized Logging System](#centralized-logging-system)
- [Centralized Localization System](#centralized-localization-system)
- [Dependency Management & Versioning](#dependency-management--versioning)
- [Startup Validation](#startup-validation)
- [Caching System (CL.Core)](#caching-system-clcore)
- [Library Lifecycle](#library-lifecycle)
- [Running the Demo Application](#running-the-demo-application)
- [Troubleshooting](#troubleshooting)

---

## 🔨 Building the Framework

### Prerequisites

- **.NET 10.0 SDK** or later
- Windows, Linux, or macOS
- Optional: Visual Studio 2025, JetBrains Rider, or VS Code

### Quick Build

**Windows (Visual Studio / Rider):**
```bash
dotnet build CodeLogic3-Windows.sln
```

**Linux / macOS / WSL:**
```bash
dotnet build CodeLogic3-Linux.sln
```

### Clean Build

```bash
dotnet clean
dotnet build
```

### Build Individual Libraries

```bash
# Build just the framework
dotnet build CodeLogic/CodeLogic.csproj

# Build a specific library
dotnet build CL.MySQL2/CL.MySQL2.csproj

# Build the demo app
dotnet build Demo.App/Demo.App.csproj
```

### Build Output Structure

```
Demo.App/bin/Debug/net10.0/
├── Demo.App.exe                    # Your application
├── Demo.App.dll
├── CodeLogic.dll                   # Framework DLL
├── CL.*.dll                        # Library DLLs
│
└── CodeLogic/                      # Runtime directory (created on first run)
    ├── CodeLogic.json              # Framework configuration
    │
    ├── Framework/                  # Framework-specific files
    │   └── logs/                   # Centralized debug logs
    │
    ├── CL.MySQL2/                  # Each library has its own folder
    │   ├── config.json             # Library configuration (auto-generated)
    │   ├── data/                   # Library data files
    │   └── logs/                   # Library-specific logs
    │
    ├── CL.Mail/
    │   ├── config.json
    │   ├── localization/           # Localization files
    │   │   ├── en-US.json
    │   │   └── de-DE.json
    │   ├── data/
    │   └── logs/
    │
    └── CL.SQLite/
        ├── config.json
        ├── data/
        │   └── databases/          # SQLite database files
        └── logs/
```

---

## 🗂️ Project Structure

### Core Framework

```
CodeLogic/                          # Core framework project
├── CodeLogic.cs                    # Main static API
├── Abstractions/                   # Interfaces (ILibrary, IPlugin, etc.)
├── Configuration/                  # Centralized configuration system
│   ├── ConfigurationManager.cs
│   ├── ConfigModelBase.cs
│   └── IConfigurationManager.cs
├── Logging/                        # Centralized logging system
│   ├── Logger.cs
│   ├── FileLogger.cs
│   └── ILogger.cs
├── Localization/                   # Centralized localization system
│   ├── LocalizationManager.cs
│   └── ILocalizationManager.cs
└── Libraries/                      # Library management
    └── LibraryManager.cs
```

### Libraries (CL.*)

All libraries follow the same structure:

```
CL.LibraryName/
├── CL.LibraryName.csproj           # Project file
├── LibraryNameLibrary.cs           # Main library class (implements ILibrary)
├── Models/
│   └── Configuration.cs            # Configuration model (extends ConfigModelBase)
├── Services/                       # Library services
└── Helpers/                        # Helper classes
```

### Demo Application

```
Demo.App/
├── Demo.App.csproj                 # Demo app project
├── Program.cs                      # Main entry point
├── Features/                       # Feature demos
│   ├── MySQL/                      # MySQL demo
│   ├── SQLite/                     # SQLite demo
│   └── NetUtils/                   # Network utilities demo
└── CodeLogic.json                  # Pre-configured framework config
```

---

## ⚙️ Centralized Configuration System

### How It Works

The framework provides **automatic configuration management** for all libraries:

1. **Registration Phase** - Libraries register configuration models during `OnConfigureAsync()`
2. **Auto-Generation** - Framework generates default config files if missing
3. **Auto-Loading** - Framework loads configurations before `OnInitializeAsync()`
4. **Validation** - Framework validates configurations before use
5. **Access Phase** - Libraries retrieve loaded configurations in `OnInitializeAsync()`

### Creating a Configuration Model

```csharp
using CodeLogic.Configuration;

namespace CL.YourLib.Models;

/// <summary>
/// Configuration for YourLib - auto-generated as config.json
/// </summary>
[ConfigSection("yourlib")]
public class YourLibConfiguration : ConfigModelBase
{
    /// <summary>
    /// Enable or disable the library
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// API endpoint URL
    /// </summary>
    public string ApiUrl { get; set; } = "https://api.example.com";

    /// <summary>
    /// Connection timeout in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// API credentials
    /// </summary>
    public CredentialsConfig Credentials { get; set; } = new();

    /// <summary>
    /// Validates the configuration
    /// </summary>
    public override ConfigValidationResult Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(ApiUrl))
            errors.Add("API URL is required");

        if (TimeoutSeconds < 1 || TimeoutSeconds > 300)
            errors.Add("Timeout must be between 1 and 300 seconds");

        if (string.IsNullOrWhiteSpace(Credentials.ApiKey))
            errors.Add("API key is required");

        return errors.Count > 0
            ? ConfigValidationResult.Invalid(errors)
            : ConfigValidationResult.Valid();
    }
}

public class CredentialsConfig
{
    public string ApiKey { get; set; } = "your-api-key-here";
    public string SecretKey { get; set; } = "your-secret-key-here";
}
```

### Using Configuration in Your Library

```csharp
using CodeLogic.Abstractions;

public class YourLibrary : ILibrary
{
    private LibraryContext? _context;
    private YourLibConfiguration? _config;

    // Phase 1: REGISTER configuration model
    public async Task OnConfigureAsync(LibraryContext context)
    {
        _context = context;

        // Register the configuration model
        // Framework will auto-generate config.json if it doesn't exist
        context.Configuration.Register<YourLibConfiguration>();

        context.Logger.Info("Configuration registered");
    }

    // Phase 2: RETRIEVE loaded configuration
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _context = context;

        // Get the loaded and validated configuration
        _config = context.Configuration.Get<YourLibConfiguration>();

        // Configuration is guaranteed to be valid here
        context.Logger.Info($"Using API endpoint: {_config.ApiUrl}");
        context.Logger.Info($"Timeout: {_config.TimeoutSeconds}s");

        // Initialize your services using the configuration
        InitializeServices(_config);
    }

    // Phase 3: START services
    public async Task OnStartAsync(LibraryContext context)
    {
        if (!_config!.Enabled)
        {
            context.Logger.Info("Library is disabled in configuration");
            return;
        }

        // Start your services
        await StartServicesAsync();
    }

    // Phase 4: STOP services
    public async Task OnStopAsync()
    {
        await StopServicesAsync();
    }
}
```

### Generated Configuration File

After first run, the framework generates: `CodeLogic/CL.YourLib/config.json`

```json
{
  "enabled": true,
  "apiUrl": "https://api.example.com",
  "timeoutSeconds": 30,
  "credentials": {
    "apiKey": "your-api-key-here",
    "secretKey": "your-secret-key-here"
  }
}
```

### Configuration Lifecycle

```
Application Start
    ↓
Framework: CodeLogic.InitializeAsync()
    ↓
Framework: CodeLogic.ConfigureAsync()
    ├─→ Discover all CL.* libraries
    ├─→ Call OnConfigureAsync() for each library
    │   └─→ Libraries register config models
    ├─→ Generate default configs (if missing)
    ├─→ Load all configurations
    ├─→ Validate all configurations
    │   └─→ FAIL if validation errors
    └─→ Call OnInitializeAsync() for each library
        └─→ Libraries retrieve loaded configs
    ↓
Application Running (configs available)
```

---

## 📊 Centralized Logging System

### How It Works

CodeLogic provides **two-tier logging**:

1. **Per-Library Logs** - Each library logs to its own directory
2. **Centralized Debug Log** - ALL libraries combined in one file (when debug enabled)

### Using the Logger in Your Library

```csharp
public async Task OnInitializeAsync(LibraryContext context)
{
    // Logger is automatically provided in LibraryContext
    context.Logger.Debug("Detailed debug information");
    context.Logger.Info("General information");
    context.Logger.Warning("Warning message");
    context.Logger.Error("Error occurred", exception);
    context.Logger.Critical("Critical failure", exception);
}
```

### Log Output Locations

**Per-Library Logs:**
```
CodeLogic/CL.YourLib/logs/2025/11/23/
├── info.log        # Info, Warning, Error, Critical
├── warning.log     # Warning, Error, Critical
├── error.log       # Error, Critical
└── critical.log    # Critical only
```

**Centralized Debug Log (when enabled):**
```
CodeLogic/Framework/logs/2025/11/23/
└── debug_all.log   # ALL libraries, ALL levels
```

### Log Format

```
2025-11-23 18:30:45.123 [INFO] Database connection established
2025-11-23 18:30:46.456 [ERROR] Query failed: Connection timeout
```

**Centralized debug log includes library prefix:**
```
2025-11-23 18:30:45 [MYSQL] [DEBUG] Connection pool initialized
2025-11-23 18:30:45 [MYSQL] [INFO] Database connected
2025-11-23 18:30:46 [MAIL] [INFO] SMTP connection established
2025-11-23 18:30:47 [MYSQL] [ERROR] Query timeout
```

### Configuring Logging

Edit `CodeLogic/CodeLogic.json`:

```json
{
  "logging": {
    "globalLevel": "Info",              // Minimum level: Debug, Info, Warning, Error, Critical
    "enableDebugMode": true,            // Enable debug features
    "centralizedDebugLog": true,        // Write to debug_all.log
    "fileNamePattern": "{date:yyyy}/{date:MM}/{date:dd}/{level}.log",
    "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff",
    "enableConsoleOutput": true,        // Also log to console
    "consoleMinimumLevel": "Info"       // Console minimum level
  }
}
```

### Log Levels

| Level | Use Case | Output Files |
|-------|----------|--------------|
| **Debug** | Detailed troubleshooting | debug_all.log (if enabled) |
| **Info** | General information | info.log, debug_all.log |
| **Warning** | Potential issues | warning.log, info.log, debug_all.log |
| **Error** | Errors that need attention | error.log, warning.log, info.log, debug_all.log |
| **Critical** | Fatal errors | critical.log, error.log, warning.log, info.log, debug_all.log |

---

## 🌍 Centralized Localization System

### How It Works

Similar to configuration, the framework provides **automatic localization management**:

1. **Registration Phase** - Libraries register localization models
2. **Auto-Generation** - Framework generates default language files if missing
3. **Auto-Loading** - Framework loads localization files
4. **Access Phase** - Libraries retrieve localized strings

### Creating a Localization Model

```csharp
using CodeLogic.Localization;

namespace CL.YourLib.Models;

/// <summary>
/// Localized strings for YourLib
/// Auto-generated as localization/en-US.json, de-DE.json, etc.
/// </summary>
[LocalizationSection("yourlib")]
public class YourLibStrings : LocalizationModelBase
{
    [LocalizedString(Description = "Library initialized message")]
    public string LibraryInitialized { get; set; } = "YourLib initialized successfully";

    [LocalizedString(Description = "Connection established message")]
    public string ConnectionEstablished { get; set; } = "Connection to API established";

    [LocalizedString(Description = "Error connecting message")]
    public string ErrorConnecting { get; set; } = "Failed to connect to API: {0}";

    [LocalizedString(Description = "Processing item message")]
    public string ProcessingItem { get; set; } = "Processing item {0} of {1}";
}
```

### Using Localization in Your Library

```csharp
public class YourLibrary : ILibrary
{
    private YourLibStrings? _strings;

    public async Task OnConfigureAsync(LibraryContext context)
    {
        // Register localization model
        context.Localization.Register<YourLibStrings>();
    }

    public async Task OnInitializeAsync(LibraryContext context)
    {
        // Get loaded localization
        _strings = context.Localization.Get<YourLibStrings>();

        // Use localized strings
        context.Logger.Info(_strings.LibraryInitialized);
    }

    public async Task ConnectToApi()
    {
        try
        {
            // ... connection logic ...
            _context.Logger.Info(_strings.ConnectionEstablished);
        }
        catch (Exception ex)
        {
            // Format with parameters
            var message = string.Format(_strings.ErrorConnecting, ex.Message);
            _context.Logger.Error(message, ex);
        }
    }

    public async Task ProcessItems(List<Item> items)
    {
        for (int i = 0; i < items.Count; i++)
        {
            var message = string.Format(_strings.ProcessingItem, i + 1, items.Count);
            _context.Logger.Info(message);

            // ... process item ...
        }
    }
}
```

### Generated Localization Files

**English (en-US.json):**
```
CodeLogic/CL.YourLib/localization/en-US.json
```
```json
{
  "libraryInitialized": "YourLib initialized successfully",
  "connectionEstablished": "Connection to API established",
  "errorConnecting": "Failed to connect to API: {0}",
  "processingItem": "Processing item {0} of {1}"
}
```

**German (de-DE.json):**
```
CodeLogic/CL.YourLib/localization/de-DE.json
```
```json
{
  "libraryInitialized": "YourLib erfolgreich initialisiert",
  "connectionEstablished": "Verbindung zur API hergestellt",
  "errorConnecting": "Verbindung zur API fehlgeschlagen: {0}",
  "processingItem": "Verarbeite Element {0} von {1}"
}
```

### Configuring Localization

Edit `CodeLogic/CodeLogic.json`:

```json
{
  "localization": {
    "defaultCulture": "en-US",
    "supportedCultures": ["en-US", "de-DE", "fr-FR"],
    "autoGenerateTemplates": true
  }
}
```

### Changing Language at Runtime

```csharp
// In your application
await context.Localization.SetCultureAsync("de-DE");

// Reload localization for all libraries
await context.Localization.ReloadAsync();
```

---

## 🔗 Dependency Management & Versioning

### Overview

CodeLogic 3.0 includes a robust dependency management system that ensures libraries are:
- **Loaded in correct order** (dependencies before dependents)
- **Version compatible** (minimum version requirements enforced)
- **Optionally required** (optional dependencies don't fail if missing)

### Defining Library Dependencies

Use the `LibraryManifestAttribute` to declare dependencies:

```csharp
using CodeLogic.Abstractions;

[LibraryManifest(
    Id = "yourlib",
    Name = "Your Library",
    Version = "3.0.0",
    Description = "Your library description",
    Author = "Your Name",
    Dependencies = new[]
    {
        // Required dependency (any version)
        LibraryDependency.Required("cl.core"),

        // Required dependency with minimum version
        LibraryDependency.Required("cl.mysql2", "2.0.0"),

        // Optional dependency (won't fail if missing)
        LibraryDependency.Optional("cl.mail")
    },
    Tags = new[] { "database", "orm", "sql" }
)]
public class YourLibrary : ILibrary
{
    // Library implementation...
}
```

### LibraryDependency Types

**Required Dependency:**
```csharp
// Must be present (any version)
LibraryDependency.Required("cl.core")

// Must be present with minimum version
LibraryDependency.Required("cl.mysql2", "2.0.0")
```

**Optional Dependency:**
```csharp
// Nice to have, but not required
LibraryDependency.Optional("cl.mail")
```

### Semantic Versioning

The framework uses **semantic versioning** (major.minor.patch):

```csharp
Version = "2.0.0"
         ↓
    Major.Minor.Patch
```

**Version Comparison:**
- `2.0.0` < `2.0.1` (patch increment)
- `2.0.1` < `2.1.0` (minor increment)
- `2.1.0` < `3.0.0` (major increment)

**Minimum Version Enforcement:**
```csharp
// Your library requires MySQL2 >= 2.0.0
Dependencies = new[]
{
    LibraryDependency.Required("cl.mysql2", "2.0.0")
}

// If loaded MySQL2 is version 1.9.5:
// ❌ FAIL: "Library 'YourLib' requires 'cl.mysql2' version >= 2.0.0, but version 1.9.5 is loaded"

// If loaded MySQL2 is version 2.0.0 or 2.1.0:
// ✅ PASS: Version requirement satisfied
```

### Dependency Resolution

The framework uses **topological sorting** to determine load order:

**Example:**
```
CL.Core (no dependencies)
    ↑
CL.Mail (depends on CL.Core)
    ↑
YourApp (depends on CL.Mail and CL.Core)

Load Order: CL.Core → CL.Mail → YourApp
```

**Circular Dependency Detection:**
```
Library A depends on B
Library B depends on C
Library C depends on A  ← Circular!

Result: ❌ "Circular dependency detected: A → B → C → A"
```

### Configuration

Enable/disable dependency resolution in `CodeLogic/CodeLogic.json`:

```json
{
  "libraries": {
    "enableDependencyResolution": true,    // Enable topological sorting
    "enableVersionChecking": true          // Enforce version requirements
  }
}
```

**When Disabled:**
- Libraries load in discovery order (alphabetical)
- No version checking
- No circular dependency detection

### Validation Errors

The framework validates dependencies during startup:

**Missing Required Dependency:**
```
❌ Library 'YourLib' requires missing dependency: 'cl.mysql2' (>= 2.0.0)
```

**Version Too Old:**
```
❌ Library 'YourLib' requires 'cl.mysql2' version >= 2.0.0, but version 1.9.5 is loaded
```

**Circular Dependency:**
```
❌ Circular dependency detected: libA → libB → libC → libA
```

**Optional Dependency Missing:**
```
ℹ️ Optional dependency 'cl.mail' not found (library will continue)
```

---

## ✅ Startup Validation

### Overview

Before loading any libraries, the framework performs **pre-flight checks** to ensure the system is ready:

- **Directory Write Access** - All required directories are writable
- **System Requirements** - .NET version, memory, disk space
- **Configuration Validity** - CodeLogic.json exists and is valid JSON

### Validation Steps

```
Application Start
    ↓
CodeLogic.ConfigureAsync()
    ↓
→ Validating system...
    ├─→ Check directory write access
    │   ├─→ CodeLogic/
    │   ├─→ CodeLogic/Framework/
    │   └─→ CodeLogic/Framework/logs/
    │
    ├─→ Check system requirements
    │   ├─→ .NET version (recommended: 10.0+)
    │   ├─→ Available memory
    │   └─→ Available disk space
    │
    └─→ Check CodeLogic.json
        ├─→ File exists
        └─→ Valid JSON format
    ↓
✓ System validation passed
    ↓
Continue with library loading...
```

### Validation Output

**Success:**
```
→ Validating system...
  ✓ System validation passed

→ Discovering libraries...
```

**Failure:**
```
→ Validating system...
  ✗ Startup validation failed:
  - Directory 'CodeLogic/Framework/logs' is not writable
  - Insufficient disk space (required: 100MB, available: 50MB)

❌ Fatal error: Startup validation failed
```

**Warnings (non-fatal):**
```
→ Validating system...
  ⚠ Running on .NET 8.0. Framework is optimized for .NET 10+
  ⚠ Low available memory (less than 1GB)
  ✓ System validation passed
```

### What Gets Validated

**Directory Access:**
- Creates test file in each required directory
- Tests write permissions
- Automatically creates missing directories
- Fails if any directory is not writable

**System Requirements:**
- **.NET Version**: Warns if below 10.0
- **Available Memory**: Warns if below 1GB
- **Available Disk Space**: Fails if below 100MB

**Configuration File:**
- Checks `CodeLogic/CodeLogic.json` exists
- Validates JSON syntax
- Does NOT validate content (that happens later)

### Customizing Validation

The startup validator can be extended in `CodeLogic/Utilities/StartupValidator.cs`:

```csharp
public class StartupValidator
{
    public ValidationResult Validate(string rootDirectory)
    {
        ValidateDirectories(rootDirectory);
        ValidateSystemRequirements();
        ValidateCodeLogicJson(rootDirectory);

        // Add custom validations here
        ValidateCustomRequirement();

        return new ValidationResult
        {
            IsSuccess = _errors.Count == 0,
            ErrorMessage = _errors.Count > 0 ? string.Join("\n", _errors) : null
        };
    }

    private void ValidateCustomRequirement()
    {
        // Your custom validation logic
        if (!CheckSomething())
        {
            _errors.Add("Custom requirement not met");
        }
    }
}
```

---

## 💾 Caching System (CL.Core)

### Overview

**CL.Core** now includes a built-in **thread-safe in-memory caching system** for high-performance data caching across your application.

### Features

- **Thread-Safe**: Concurrent dictionary with atomic operations
- **Expiration Support**: Automatic expiry based on time
- **Size Limits**: Max item count with automatic eviction
- **Auto-Cleanup**: Background task removes expired entries
- **Statistics**: Track cache hits, misses, and hit ratio
- **Generic API**: Type-safe caching for any object

### Using the Cache

**Get CL.Core Library:**
```csharp
// In your application
var coreLib = CodeLogic.GetLibrary<CoreLibrary>();
```

**Create Cache Instance:**
```csharp
using CL.Core.Utilities.Caching;

// Default options
var cache = new MemoryCache();

// Custom options
var cache = new MemoryCache(new CacheOptions
{
    MaxItems = 5000,                            // Max 5000 items
    DefaultExpiration = TimeSpan.FromMinutes(30), // 30 min default TTL
    AutoCleanupInterval = TimeSpan.FromMinutes(5) // Cleanup every 5 min
});
```

### Basic Operations

**Store Value:**
```csharp
// Store with default expiration
await cache.SetAsync("user:123", userData);

// Store with custom expiration
await cache.SetAsync("session:abc", sessionData, TimeSpan.FromHours(2));

// Store without expiration (cache forever)
await cache.SetAsync("config:main", configData, expiration: null);
```

**Retrieve Value:**
```csharp
// Get value (returns null if not found or expired)
var user = await cache.GetAsync<UserData>("user:123");

if (user != null)
{
    // Use cached data
    Console.WriteLine($"Cache hit: {user.Name}");
}
else
{
    // Cache miss - load from database
    user = await database.GetUserAsync(123);
    await cache.SetAsync("user:123", user, TimeSpan.FromMinutes(10));
}
```

**Check Existence:**
```csharp
if (await cache.ExistsAsync("user:123"))
{
    // Key exists and is not expired
}
```

**Remove Value:**
```csharp
await cache.RemoveAsync("user:123");
```

**Clear All:**
```csharp
await cache.ClearAsync();
```

### Get-or-Create Pattern

Most efficient caching pattern:

```csharp
// Try cache first, load from source if missing
var user = await cache.GetOrCreateAsync(
    key: "user:123",
    factory: async () => {
        // This only runs on cache miss
        return await database.GetUserAsync(123);
    },
    expiration: TimeSpan.FromMinutes(10)
);
```

**Example Usage:**
```csharp
public async Task<ProductData> GetProductAsync(int productId)
{
    return await cache.GetOrCreateAsync(
        $"product:{productId}",
        async () => {
            // Load from database (cache miss)
            _logger.Info($"Loading product {productId} from database");
            return await _database.GetProductAsync(productId);
        },
        TimeSpan.FromMinutes(15)
    );
}
```

### Cache Statistics

Track cache performance:

```csharp
var stats = cache.GetStatistics();

Console.WriteLine($"Items: {stats.ItemCount}");
Console.WriteLine($"Hits: {stats.Hits}");
Console.WriteLine($"Misses: {stats.Misses}");
Console.WriteLine($"Hit Ratio: {stats.HitRatio:F2}%");
```

**Example Output:**
```
Items: 1247
Hits: 8923
Misses: 1234
Hit Ratio: 87.85%
```

### Cache Options

```csharp
public class CacheOptions
{
    // Maximum number of items (0 = unlimited)
    public int MaxItems { get; set; } = 10000;

    // Default expiration time (null = no expiration)
    public TimeSpan? DefaultExpiration { get; set; } = TimeSpan.FromHours(1);

    // Auto-cleanup interval (null = no auto-cleanup)
    public TimeSpan? AutoCleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
```

### Advanced Usage

**Different Cache Instances:**
```csharp
// Separate caches for different data types
var userCache = new MemoryCache(new CacheOptions
{
    MaxItems = 10000,
    DefaultExpiration = TimeSpan.FromMinutes(30)
});

var productCache = new MemoryCache(new CacheOptions
{
    MaxItems = 50000,
    DefaultExpiration = TimeSpan.FromHours(2)
});

var sessionCache = new MemoryCache(new CacheOptions
{
    MaxItems = 5000,
    DefaultExpiration = TimeSpan.FromMinutes(15)
});
```

**Cache Namespacing:**
```csharp
// Use prefixes for logical separation
await cache.SetAsync("user:123", userData);
await cache.SetAsync("product:456", productData);
await cache.SetAsync("session:abc", sessionData);
```

**Bulk Operations:**
```csharp
// Cache multiple items
var tasks = users.Select(u =>
    cache.SetAsync($"user:{u.Id}", u, TimeSpan.FromMinutes(10))
);
await Task.WhenAll(tasks);

// Retrieve multiple items
var userIds = new[] { 123, 456, 789 };
var tasks = userIds.Select(id =>
    cache.GetAsync<UserData>($"user:{id}")
);
var users = await Task.WhenAll(tasks);
```

### Performance Considerations

**When to Use Cache:**
- ✅ Frequently accessed data
- ✅ Expensive to compute/fetch
- ✅ Doesn't change often
- ✅ Can tolerate slight staleness

**When NOT to Use Cache:**
- ❌ Data changes frequently
- ❌ Memory constrained environment
- ❌ Must be 100% fresh
- ❌ Rarely accessed data

**Best Practices:**
1. **Set appropriate expiration times** - Balance freshness vs performance
2. **Use namespacing** - Organize keys logically (`user:123`, `product:456`)
3. **Monitor statistics** - Track hit ratio to verify effectiveness
4. **Set size limits** - Prevent unbounded memory growth
5. **Use GetOrCreate** - Simplest and most efficient pattern

### Example: Complete Caching Service

```csharp
public class UserService
{
    private readonly ICache _cache;
    private readonly IDatabase _database;
    private readonly ILogger _logger;

    public UserService(IDatabase database, ILogger logger)
    {
        _database = database;
        _logger = logger;

        // Create cache with custom settings
        _cache = new MemoryCache(new CacheOptions
        {
            MaxItems = 10000,
            DefaultExpiration = TimeSpan.FromMinutes(15),
            AutoCleanupInterval = TimeSpan.FromMinutes(5)
        });
    }

    public async Task<UserData> GetUserAsync(int userId)
    {
        return await _cache.GetOrCreateAsync(
            $"user:{userId}",
            async () => {
                _logger.Debug($"Cache miss - loading user {userId} from database");
                return await _database.GetUserAsync(userId);
            },
            TimeSpan.FromMinutes(15)
        );
    }

    public async Task UpdateUserAsync(UserData user)
    {
        // Update database
        await _database.UpdateUserAsync(user);

        // Invalidate cache
        await _cache.RemoveAsync($"user:{user.Id}");

        _logger.Info($"Updated user {user.Id} and invalidated cache");
    }

    public CacheStatistics GetCacheStats()
    {
        return _cache.GetStatistics();
    }
}
```

---

## 🔄 Library Lifecycle

Understanding the library lifecycle is crucial for proper development:

### Phase 1: OnConfigureAsync()

**Purpose:** Register configuration and localization models

```csharp
public async Task OnConfigureAsync(LibraryContext context)
{
    _context = context;

    // Register configuration model
    context.Configuration.Register<YourLibConfiguration>();

    // Register localization model
    context.Localization.Register<YourLibStrings>();

    // DO NOT access config or localization here - they haven't been loaded yet!
    // DO NOT initialize services - wait for OnInitializeAsync

    context.Logger.Info("Library configured");
}
```

**Available in Context:**
- ✅ Logger
- ✅ LibraryDirectory
- ✅ DataDirectory
- ❌ Configuration (not loaded yet)
- ❌ Localization (not loaded yet)

### Phase 2: OnInitializeAsync()

**Purpose:** Retrieve loaded configurations and initialize services

```csharp
public async Task OnInitializeAsync(LibraryContext context)
{
    _context = context;

    // NOW you can retrieve loaded configurations
    _config = context.Configuration.Get<YourLibConfiguration>();
    _strings = context.Localization.Get<YourLibStrings>();

    // Configuration is guaranteed to be:
    // ✅ Loaded from disk
    // ✅ Validated
    // ✅ Ready to use

    context.Logger.Info(_strings.LibraryInitialized);

    // Initialize your services using the configuration
    _apiClient = new ApiClient(_config.ApiUrl, _config.TimeoutSeconds);
    _database = new Database(_config.ConnectionString);

    // DO NOT start services yet - wait for OnStartAsync
}
```

**Available in Context:**
- ✅ Logger
- ✅ LibraryDirectory
- ✅ DataDirectory
- ✅ Configuration (loaded and validated)
- ✅ Localization (loaded)

### Phase 3: OnStartAsync()

**Purpose:** Start services and begin operations

```csharp
public async Task OnStartAsync(LibraryContext context)
{
    _context = context;

    // Check if library is enabled
    if (!_config!.Enabled)
    {
        context.Logger.Info("Library is disabled");
        return;
    }

    // NOW start your services
    await _apiClient.ConnectAsync();
    await _database.OpenConnectionAsync();

    // Start background tasks
    _backgroundTask = Task.Run(BackgroundWorkerAsync);

    context.Logger.Info(_strings.LibraryStarted);
}
```

### Phase 4: OnStopAsync()

**Purpose:** Graceful shutdown and cleanup

```csharp
public async Task OnStopAsync()
{
    _context?.Logger.Info("Stopping library...");

    // Stop background tasks
    _cancellationTokenSource?.Cancel();
    await _backgroundTask;

    // Close connections
    await _apiClient?.DisconnectAsync();
    await _database?.CloseConnectionAsync();

    // Dispose resources
    _apiClient?.Dispose();
    _database?.Dispose();

    _context?.Logger.Info("Library stopped");
}
```

### Complete Lifecycle Flow

```
Application Starts
    ↓
CodeLogic.InitializeAsync()
    ├─→ Create directory structure
    ├─→ Load CodeLogic.json
    └─→ Check first run
    ↓
CodeLogic.ConfigureAsync()
    ├─→ Discover CL.* libraries
    │
    ├─→ For each library:
    │   ├─→ Call OnConfigureAsync()
    │   │   └─→ Library registers models
    │   │
    │   ├─→ Generate default configs (if missing)
    │   ├─→ Load configurations
    │   ├─→ Validate configurations
    │   │
    │   └─→ Call OnInitializeAsync()
    │       └─→ Library retrieves configs and initializes
    ↓
CodeLogic.StartAsync()
    └─→ For each library:
        └─→ Call OnStartAsync()
            └─→ Library starts services
    ↓
Application Running
    ↓
CodeLogic.StopAsync()
    └─→ For each library (reverse order):
        └─→ Call OnStopAsync()
            └─→ Library stops services
    ↓
Application Exits
```

---

## 🚀 Running the Demo Application

### First Run

1. **Build the demo:**
   ```bash
   dotnet build Demo.App/Demo.App.csproj
   ```

2. **Run the demo:**
   ```bash
   cd Demo.App/bin/Debug/net10.0
   ./Demo.App
   ```

3. **Framework initializes and exits:**
   ```
   ════════════════════════════════════════════════════════
      CodeLogic 3.0 Framework
   ════════════════════════════════════════════════════════

   → Initializing framework...
     ✓ Created directory structure
     ✓ Generated CodeLogic.json
   ✓ Framework initialized

   → Discovering libraries...
     ✓ Discovered: MySQL2 Database Library v2.0.0
     ✓ Discovered: CL.SQLite v3.0.0
     ✓ Discovered: Network Utilities Library v3.0.0
     ...

   → Configuring 6 libraries...
     ✓ Configured: Core Utilities Library
     ✗ Failed to configure MySQL2: Configuration validation failed

   ❌ Fatal error: Configuration validation failed:
      Database host is required
   ```

4. **Configure the libraries:**

   Edit the generated config files in `CodeLogic/CL.*/config.json`:

   **MySQL Example:**
   ```json
   {
     "enabled": true,
     "host": "localhost",
     "port": 3306,
     "database": "myapp",
     "username": "root",
     "password": "password"
   }
   ```

5. **Run again - Success!**
   ```bash
   ./Demo.App
   ```

   ```
   ════════════════════════════════════════════════════════
      CodeLogic 3.0 Framework
   ════════════════════════════════════════════════════════

   → Initializing framework...
     ✓ Loaded CodeLogic.json
   ✓ Framework initialized

   → Discovering libraries...
     ✓ Discovered: 6 libraries

   → Configuring 6 libraries...
     ✓ Configured: All libraries

   → Initializing libraries...
     ✓ Initialized: All libraries

   → Starting libraries...
     ✓ Started: All libraries
   ✓ All libraries started

   ════════════════════════════════════════════════════════
      DEMO APPLICATION MENU
   ════════════════════════════════════════════════════════

   Choose a demo:
   1. MySQL Demo (CRUD operations)
   2. SQLite Demo (Full feature demo)
   3. Network Utilities Demo
   ...
   ```

### Demo Features

The demo application showcases:

- **MySQL2 Demo** - Full CRUD operations with ORM
- **SQLite Demo** - Table sync, QueryBuilder, batch operations
- **Network Utilities Demo** - IP geolocation, DNSBL checking
- **Configuration Management** - Auto-generation and validation
- **Logging System** - Per-library and centralized logs
- **Localization** - Multi-language support

---

## 🔧 Troubleshooting

### Build Errors

**Error: Could not find CodeLogic.dll**
```bash
# Build CodeLogic framework first
dotnet build CodeLogic/CodeLogic.csproj
```

**Error: Project file does not exist**
```bash
# Use correct solution file for your platform
# Windows:
dotnet build CodeLogic3-Windows.sln
# Linux/Mac:
dotnet build CodeLogic3-Linux.sln
```

### Runtime Errors

**Error: Configuration validation failed**

This is **expected behavior**! The framework:
1. Auto-generates config files with placeholder values
2. Validates them
3. Exits if validation fails

**Solution:** Edit the config files in `CodeLogic/CL.*/config.json` with your actual values.

**Error: Library not discovered**

Check:
- Library DLL is in `bin/Debug/net10.0/` directory
- Library name starts with `CL.`
- Library implements `ILibrary` interface
- Library has `[LibraryManifest]` attribute

**Error: Config file not generated**

The config is only generated if:
- Library is discovered
- `OnConfigureAsync()` calls `context.Configuration.Register<T>()`
- Config file doesn't already exist

Delete the config file and run again to regenerate.

### Log Issues

**No logs appearing:**

Check `CodeLogic/CodeLogic.json`:
```json
{
  "logging": {
    "globalLevel": "Info",           // Set to "Debug" for more logs
    "enableConsoleOutput": true      // Enable console output
  }
}
```

**Centralized debug log not created:**

Enable it in `CodeLogic/CodeLogic.json`:
```json
{
  "logging": {
    "enableDebugMode": true,
    "centralizedDebugLog": true
  }
}
```

### Clean Start

To completely reset:

```bash
# Clean build
dotnet clean
dotnet build

# Delete runtime directory
rm -rf Demo.App/bin/Debug/net10.0/CodeLogic

# Run again
cd Demo.App/bin/Debug/net10.0
./Demo.App
```

---

## 📚 Additional Resources

- **README.md** - Framework overview and quick start
- **CodeLogic/Abstractions/** - Interface documentation
- **Demo.App/Features/** - Working examples for each library

---

## ✅ Summary

### Key Concepts

1. **Centralized Configuration** - Register models, framework handles generation/loading/validation
2. **Centralized Logging** - Per-library logs + centralized debug log
3. **Centralized Localization** - Register models, framework handles generation/loading
4. **Dependency Management** - Topological sorting, version requirements, optional dependencies
5. **Startup Validation** - Pre-flight checks for directories, system requirements, and configuration
6. **Caching System** - Thread-safe in-memory cache with expiration and statistics (CL.Core)
7. **Library Lifecycle** - Configure → Initialize → Start → Stop

### Development Workflow

1. Create library project
2. Implement `ILibrary` interface
3. Create configuration model (extends `ConfigModelBase`)
4. Register models in `OnConfigureAsync()`
5. Retrieve loaded configs in `OnInitializeAsync()`
6. Start services in `OnStartAsync()`
7. Stop services in `OnStopAsync()`
8. Build and run!

### First Run Flow

1. Run application
2. Framework generates configs with defaults
3. Framework validates (may fail)
4. Application exits
5. Edit configs with real values
6. Run again - Success! ✅

---

**Happy Coding! 🚀**
