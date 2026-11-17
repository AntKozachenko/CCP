namespace Core.Configuration;

// Лаб. 9. Типізовані налаштування. Core нічого не знає про JSON-файли, user-secrets чи змінні середовища:
// клас лише описує форму даних, а наповнює його config.Get<AppOptions>() у Cli.
// Secret (Integration:ApiKey) навмисно НЕ входить сюди: він читається як config["Integration:ApiKey"].
public sealed class AppOptions
{
    public StorageOptions Storage { get; init; } = new();
    public LoggingOptions Logging { get; init; } = new();
    public OrdersOptions Orders { get; init; } = new();
    public string EnvironmentName { get; init; } = "Development";
}

// Від чого рахувати відносний шлях до даних.
public enum StorageRoot
{
    AppBase,        // AppContext.BaseDirectory (поруч зі збіркою)
    LocalAppData    // Environment.SpecialFolder.LocalApplicationData (дані користувача, переживають перезбірку)
}

public sealed class StorageOptions
{
    public string DataPath { get; init; } = "data";
    public string FileName { get; init; } = "catalog.json";
    public string LogPath { get; init; } = "logs";
    public StorageRoot Root { get; init; } = StorageRoot.AppBase;

    // Проста змінна CROSSAPP_DATA_PATH перекриває лише DataPath; решта розділу лишається як є.
    public StorageOptions WithDataPath(string? overridePath) => string.IsNullOrWhiteSpace(overridePath)
        ? this
        : new StorageOptions { DataPath = overridePath, FileName = FileName, LogPath = LogPath, Root = Root };

    // Абсолютний DataPath береться як є, відносний будується через Path.Combine від baseDir.
    // Ніякої конкатенації рядків із "\" чи "/": роздільник підставляє ОС.
    public string ResolveDataDirectory(string baseDir) => ResolveDirectory(DataPath, baseDir);

    public string ResolveLogDirectory(string baseDir) => ResolveDirectory(LogPath, baseDir);

    public string ResolveFullPath(string baseDir)
    {
        var root = ResolveDataDirectory(baseDir);
        Directory.CreateDirectory(root);
        return Path.GetFullPath(Path.Combine(root, FileName));
    }

    private static string ResolveDirectory(string path, string baseDir) =>
        Path.GetFullPath(Path.IsPathRooted(path) ? path : Path.Combine(baseDir, path));
}

public enum AppLogLevel
{
    Trace = 0,
    Debug = 1,
    Information = 2,
    Warning = 3,
    Error = 4
}

public sealed class LoggingOptions
{
    public AppLogLevel MinimumLevel { get; init; } = AppLogLevel.Information;
}

// Варіант «Замовлення»: ключ Orders:Currency.
public sealed class OrdersOptions
{
    public string Currency { get; init; } = "UAH";
}
