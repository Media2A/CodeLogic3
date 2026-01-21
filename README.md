# CodeLogic 3.0 - Micro-Service Plugin Framework

**Version:** 3.0.0
**Status:** ✅ Production Ready
**License:** MIT

---

## 🚀 What is CodeLogic 3.0?

CodeLogic 3.0 is a **modern, micro-service style plugin framework** for .NET applications that provides:

- ✅ **Complete Isolation** - Each library/plugin in its own directory with config, data, logs
- ✅ **Static API** - Simple global access, no DI complexity
- ✅ **Dual Logging** - Per-component logs + centralized debug log
- ✅ **Hot-Pluggable Plugins** - Load/unload/reload at runtime
- ✅ **First-Run Setup** - Auto-initialization on first run
- ✅ **Clean Output Folder** - Everything in `CodeLogic/` subdirectory
- ✅ **Type-Safe** - No magic strings, full IntelliSense

---

## 📁 Folder Structure (Micro-Service Style)

```
YourApp/
├── YourApp.exe
├── YourApp.dll
├── .codelogic                 # First-run marker
│
├── CodeLogic/                 # All CodeLogic files isolated here!
│   ├── CodeLogic.json         # Global configuration
│   │
│   ├── CL.MySQL/              # MySQL library (micro-service)
│   │   ├── CL.MySQL.dll
│   │   ├── config.json        # Library config
│   │   ├── localization/
│   │   │   ├── en-US.json
│   │   │   └── de-DE.json
│   │   ├── data/              # Library data
│   │   │   ├── migrations/
│   │   │   └── backups/
│   │   └── logs/              # Library logs
│   │       └── 2025/11/22/
│   │           ├── info.log
│   │           └── error.log
│   │
│   ├── CL.Mail/               # Mail library (micro-service)
│   │   ├── CL.Mail.dll
│   │   ├── config.json
│   │   ├── data/
│   │   └── logs/
│   │
│   └── Framework/             # Framework logs
│       └── logs/
│           └── 2025/11/22/
│               └── debug_all.log  # ALL libraries combined!
│
└── Plugins/                   # Hot-pluggable plugins
    ├── DarkTheme.Plugin/
    │   ├── DarkTheme.Plugin.dll
    │   ├── config.json
    │   ├── data/
    │   └── logs/
    └── PayPal.Plugin/
        ├── PayPal.Plugin.dll
        └── ...
```

---

---

## 📖 Documentation

- **[BUILD.md](BUILD.md)** - Complete build guide, configuration system, logging, localization, and lifecycle
- **README.md** (this file) - Framework overview and quick reference

---

## ⚡ Quick Start

### 1. Create Your Application

```csharp
using CodeLogic;

class Program
{
    static async Task Main(string[] args)
    {
        // Initialize framework
        var result = await CodeLogic.InitializeAsync();

        // First run? Framework creates structure and exits
        if (result.IsFirstRun)
        {
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
            return;  // Exit to allow configuration
        }

        // Configure and start libraries
        await CodeLogic.ConfigureAsync();
        await CodeLogic.StartAsync();

        Console.WriteLine("Application running!");
        Console.ReadKey();

        // Graceful shutdown
        await CodeLogic.StopAsync();
    }
}
```

### 2. First Run

When you run your app for the first time:

```
════════════════════════════════════════════════════════
   CodeLogic 3.0 - First Run Initialization
════════════════════════════════════════════════════════

→ Creating directory structure...
  ✓ Created 4 directories

→ Generating CodeLogic.json...
  ✓ Generated CodeLogic.json

→ Creating initialization marker...
  ✓ Created .codelogic file

════════════════════════════════════════════════════════
   Initialization Complete!
════════════════════════════════════════════════════════

📁 Directory Structure:
   CodeLogic/
   ├── CodeLogic.json
   ├── Framework/
   │   └── logs/
   ├── CL.*/  (libraries will be auto-discovered here)
   └── .codelogic (initialization marker)

📝 Next Steps:
   1. Review and edit CodeLogic.json if needed
   2. Place CL.* libraries in the CodeLogic/ directory
   3. Run your application again

ℹ️  The application will now exit to allow you to configure.
   Re-run the application when ready.

Press any key to exit...
```

### 3. Second Run (Normal Operation)

After configuring, run again:

```
════════════════════════════════════════════════════════
   CodeLogic 3.0 Framework
════════════════════════════════════════════════════════

→ Initializing framework...
  ✓ Loaded CodeLogic.json
✓ Framework initialized

→ Discovering libraries...
  ✓ Discovered: MySQL Database Library v2.0.0
  ✓ Discovered: Mail Service v1.0.0

→ Configuring 2 libraries...
  ✓ Configured: MySQL Database Library
  ✓ Configured: Mail Service
✓ Libraries configured

→ Initializing libraries...
  ✓ Initialized: MySQL Database Library
  ✓ Initialized: Mail Service

→ Starting libraries...
  ✓ Started: MySQL Database Library
  ✓ Started: Mail Service
✓ All libraries started

Application running!
```

---

## 🎯 Two-Tier System

### Tier 1: CL.* Libraries (Internal)

**Purpose:** Framework-provided libraries
**API:** Static (`Libraries.Get<T>()`)
**Location:** `CodeLogic/CL.*/`
**Hot-Reload:** ❌ No (loaded at startup)
**Examples:** CL.MySQL, CL.Mail, CL.Cache

```csharp
// Get a library
var mysql = Libraries.Get<MySQL2Library>();
var repo = mysql.GetRepository<User>();

// Use it
var users = await repo.GetAllAsync();
```

### Tier 2: External Plugins (Application)

**Purpose:** User application plugins
**API:** Instance (`new PluginManager()`)
**Location:** `Plugins/*.Plugin/`
**Hot-Reload:** ✅ Yes (dynamic load/unload)
**Examples:** DarkTheme, PayPal, Analytics

```csharp
// Create plugin manager (separate from CodeLogic)
var pluginMgr = new PluginManager(new PluginOptions
{
    PluginsDirectory = "Plugins",
    EnableHotReload = true,
    WatchForChanges = true  // Auto-reload on file change
});

// Load all plugins
await pluginMgr.LoadAllAsync();

// Use a plugin
var theme = pluginMgr.GetPlugin<IThemePlugin>("dark-theme");

// Hot-reload a plugin
await pluginMgr.ReloadPluginAsync("dark-theme");

// Unload a plugin
await pluginMgr.UnloadPluginAsync("dark-theme");
```

---

## 📝 Creating a Library (CL.*)

```csharp
using CodeLogic.Abstractions;
using CodeLogic.Configuration;
using CodeLogic.Localization;

[LibraryManifest(
    Id = "mysql",
    Name = "MySQL Database Library",
    Version = "2.0.0",
    Description = "MySQL database access",
    Author = "Your Name"
)]
public class MySQL2Library : ILibrary
{
    private DatabaseConfig? _config;
    private MySQLStrings? _strings;

    // Phase 1: Register models
    public async Task OnConfigureAsync(LibraryContext context)
    {
        context.Configuration.Register<DatabaseConfig>();
        context.Localization.Register<MySQLStrings>();
    }

    // Phase 2: Setup services
    public async Task OnInitializeAsync(LibraryContext context)
    {
        _config = context.Configuration.Get<DatabaseConfig>();
        _strings = context.Localization.Get<MySQLStrings>();

        // context.LibraryDirectory = "CodeLogic/CL.MySQL/"
        // context.DataDirectory = "CodeLogic/CL.MySQL/data/"
        // context.LogsDirectory = "CodeLogic/CL.MySQL/logs/"

        context.Logger.Info(_strings.LibraryInitialized);
    }

    // Phase 3: Start services
    public async Task OnStartAsync(LibraryContext context)
    {
        // Start your services
        context.Logger.Info(_strings.LibraryStarted);
    }

    // Phase 4: Stop gracefully
    public async Task OnStopAsync()
    {
        // Cleanup
    }

    public void Dispose() { }

    public Task<HealthStatus> HealthCheckAsync()
    {
        return Task.FromResult(HealthStatus.Healthy("All systems operational"));
    }
}
```

### Library Configuration Model

```csharp
[ConfigSection("mysql")]
public class DatabaseConfig : ConfigModelBase
{
    [Required]
    public string Host { get; set; } = "localhost";

    [Range(1, 65535)]
    public int Port { get; set; } = 3306;

    public string Database { get; set; } = "";

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Database))
            return ValidationResult.Invalid("Database name required");

        return ValidationResult.Valid();
    }
}
```

Auto-generated as: `CodeLogic/CL.MySQL/config.json`

### Library Localization Model

```csharp
[LocalizationSection("mysql")]
public class MySQLStrings : LocalizationModelBase
{
    [LocalizedString(Description = "Library initialized message")]
    public string LibraryInitialized { get; set; } = "MySQL library initialized";

    [LocalizedString(Description = "Library started message")]
    public string LibraryStarted { get; set; } = "MySQL library started";
}
```

Auto-generated as: `CodeLogic/CL.MySQL/localization/en-US.json`

---

## 🔌 Creating a Plugin

```csharp
using CodeLogic.Abstractions;

[PluginManifest(
    Id = "dark-theme",
    Name = "Dark Theme",
    Version = "1.0.0",
    Description = "Dark color scheme for the application",
    Author = "Your Name"
)]
public class DarkThemePlugin : IPlugin
{
    public string Id => "dark-theme";
    public string Name => "Dark Theme";
    public string Version => "1.0.0";
    public string? Description => "Dark color scheme";
    public string? Author => "Your Name";
    public bool IsLoaded { get; private set; }

    public async Task OnLoadAsync(PluginContext context)
    {
        // context.PluginDirectory = "Plugins/DarkTheme.Plugin/"
        // context.DataDirectory = "Plugins/DarkTheme.Plugin/data/"

        context.Logger.Info("Dark theme plugin loaded");
        IsLoaded = true;
    }

    public async Task OnUnloadAsync()
    {
        IsLoaded = false;
    }

    public void Dispose() { }

    public Task<HealthStatus> HealthCheckAsync()
    {
        return Task.FromResult(HealthStatus.Healthy());
    }
}
```

---

## 📊 Dual Logging System

### Per-Component Logs

Each library/plugin logs to its own directory:

```
CodeLogic/CL.MySQL/logs/2025/11/22/
├── info.log       # Info level and above
├── warning.log    # Warnings
└── error.log      # Errors
```

### Centralized Debug Log (When Debug Enabled)

**ALL** libraries combined in one file:

```
CodeLogic/Framework/logs/2025/11/22/debug_all.log
```

**Example content:**
```
2025-11-22 10:30:14 [MYSQL] [DEBUG] Connection pool initialized
2025-11-22 10:30:15 [MYSQL] [INFO] Database connection established
2025-11-22 10:30:15 [MAIL] [INFO] SMTP connection established
2025-11-22 10:30:20 [MYSQL] [ERROR] Query failed: Timeout
```

### Configuration

In `CodeLogic.json`:

```json
{
  "logging": {
    "globalLevel": "Info",
    "enableDebugMode": true,          // Enable debug features
    "centralizedDebugLog": true,      // Write to debug_all.log
    "enableConsoleOutput": true,
    "consoleMinimumLevel": "Info"
  }
}
```

---

## ⚙️ CodeLogic.json

```json
{
  "framework": {
    "name": "CodeLogic",
    "version": "3.0.0",
    "rootDirectory": "CodeLogic"
  },
  "logging": {
    "globalLevel": "Info",
    "enableDebugMode": false,
    "centralizedDebugLog": false,
    "fileNamePattern": "{date:yyyy}/{date:MM}/{date:dd}/{level}.log",
    "timestampFormat": "yyyy-MM-dd HH:mm:ss.fff",
    "enableConsoleOutput": true,
    "consoleMinimumLevel": "Info"
  },
  "localization": {
    "defaultCulture": "en-US",
    "supportedCultures": ["en-US", "de-DE"],
    "autoGenerateTemplates": true
  },
  "libraries": {
    "autoDiscover": true,
    "discoveryPattern": "CL.*",
    "autoLoad": true,
    "enableDependencyResolution": true
  }
}
```

---

## 🔥 Key Features

### 1. Micro-Service Isolation

Each library is completely self-contained:
- Own DLL and dependencies
- Own configuration
- Own data directory
- Own logs

**Benefits:**
- Easy to deploy (copy folder)
- Easy to debug (all files in one place)
- No conflicts between libraries
- Can version control per-library

### 2. First-Run Initialization

Framework detects first run and:
- Creates directory structure
- Generates `CodeLogic.json`
- Creates `.codelogic` marker
- **Exits app** to allow configuration

No manual setup required!

### 3. Static API

Simple global access:

```csharp
// Framework
await CodeLogic.InitializeAsync();
await CodeLogic.ConfigureAsync();
await CodeLogic.StartAsync();

// Libraries
var mysql = Libraries.Get<MySQL2Library>();
```

No DI container required!

### 4. Hot-Pluggable Plugins

```csharp
var pluginMgr = new PluginManager();

// Load
await pluginMgr.LoadPluginAsync("path/to/plugin.dll");

// Reload (unload + load)
await pluginMgr.ReloadPluginAsync("plugin-id");

// Unload
await pluginMgr.UnloadPluginAsync("plugin-id");

// Auto-reload on file change
var pluginMgr = new PluginManager(new PluginOptions
{
    WatchForChanges = true
});
```

### 5. Dependency Resolution

Libraries can depend on each other:

```csharp
[LibraryManifest(
    Id = "email",
    Dependencies = new[] { "mysql", "cache" }
)]
public class EmailLibrary : ILibrary { }
```

Framework loads in correct order!

---

## 🎓 Complete Example

See `Example.App/Program.cs` for a full working example with:
- First-run detection
- Library loading
- Plugin system
- Command-line interface
- Hot-reload commands
- Graceful shutdown

---

## 📚 API Reference

### Static CodeLogic API

| Method | Description |
|--------|-------------|
| `InitializeAsync()` | Initialize framework (detects first run) |
| `ConfigureAsync()` | Discover and configure CL.* libraries |
| `StartAsync()` | Start all libraries |
| `StopAsync()` | Stop all libraries |
| `GetConfiguration()` | Get framework configuration |
| `GetOptions()` | Get framework options |

### Static Libraries API

| Method | Description |
|--------|-------------|
| `Get<T>()` | Get library by type |
| `Get(string id)` | Get library by ID |
| `GetAll()` | Get all loaded libraries |

### PluginManager API

| Method | Description |
|--------|-------------|
| `LoadPluginAsync(path)` | Load a plugin |
| `UnloadPluginAsync(id)` | Unload a plugin |
| `ReloadPluginAsync(id)` | Reload a plugin (hot-reload) |
| `LoadAllAsync()` | Load all discovered plugins |
| `UnloadAllAsync()` | Unload all plugins |
| `GetPlugin<T>(id)` | Get plugin by ID and type |
| `GetAllPlugins()` | Get all loaded plugins |

---

## 🚀 Building Your First Library

1. Create project: `CL.YourLib.csproj`
2. Reference: `CodeLogic.csproj`
3. Implement: `ILibrary` interface
4. Build to: `CodeLogic/CL.YourLib/`
5. Run your app!

Framework will:
- ✅ Discover your library
- ✅ Generate `config.json`
- ✅ Generate localization files
- ✅ Load and start it

---

## 📄 License

MIT License - Use however you want!

---

## 🎉 Ready to Use!

CodeLogic 3.0 is production-ready with:
- ✅ Clean micro-service architecture
- ✅ First-run initialization
- ✅ Dual logging system
- ✅ Hot-pluggable plugins
- ✅ Static API (no DI complexity)
- ✅ Complete isolation per component

**Build amazing modular applications! 🚀**

---

## 🛠️ Solution Files for Different Platforms

### Windows (Visual Studio / JetBrains Rider)
```bash
dotnet build CodeLogic3-Windows.sln
```
Uses Windows path separators (`\`) for full compatibility with Visual Studio and JetBrains Rider on Windows.

### Linux / macOS / WSL
```bash
dotnet build CodeLogic3-Linux.sln
```
Uses Unix path separators (`/`) for compatibility with dotnet CLI and IDEs on Linux/macOS.

**Both solution files include:**
- CodeLogic (Core Framework)
- 11 CL.* Libraries (All converted to CodeLogic 3.0)
- Demo.App (Demonstration application)

---

## 📦 Library Status

| Library | Status | Description |
|---------|--------|-------------|
| **CL.Core** | ✅ Ready | 40+ utility functions (image, web, security, networking) |
| **CL.GitHelper** | ✅ Ready | Git repository management with LibGit2Sharp |
| **CL.Mail** | ✅ Ready | SMTP email library with template system |
| **CL.MySQL2** | ✅ Ready | MySQL ORM with LINQ-like queries |
| **CL.NetUtils** | ✅ Ready | DNSBL checking & IP geolocation (MaxMind) |
| **CL.TwoFactorAuth** | ✅ Ready | TOTP 2FA authentication with QR codes |
| **CL.SystemStats** | ✅ Ready | CPU, memory, disk, network monitoring |
| **CL.SocialConnect** | ✅ Ready | Discord webhooks & Steam API integration |
| **CL.SQLite** | ✅ Ready | SQLite ORM with CRUD, QueryBuilder, TableSync |
| **CL.PostgreSQL** | ✅ Ready | PostgreSQL ORM with full CodeLogic 3.0 support |
| **CL.StorageS3** | ✅ Ready | AWS S3 storage with CodeLogic 3.0 support |

**All 11 libraries have been successfully converted to CodeLogic 3.0!**

---

## 🎓 Learning Resources

- **Demo.App** - Comprehensive demonstration application
  - MySQL2 Demo - Full CRUD operations
  - SQLite Demo - Table sync, QueryBuilder, batch operations
  - Network Utilities Demo - IP geolocation, DNSBL checking
- **BUILD.md** - Detailed guide for building and configuring
- **Example Libraries** - See `CL.*/` directories for implementation patterns
