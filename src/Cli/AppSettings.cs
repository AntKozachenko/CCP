using System.Reflection;
using Core.Configuration;
using Microsoft.Extensions.Configuration;

namespace Cli;

// Лаб. 9. Збирання конфігурації: пізніше джерело перекриває раніше.
// appsettings.json → appsettings.{Env}.json → user-secrets → змінні CROSSAPP_* → аргументи --Ключ:Підключ=значення
public sealed record AppSettings(
    IConfiguration Config,
    AppOptions Options,
    StorageOptions Storage,
    string BaseDir,
    string DataDir,
    string DataFile,
    string LogDir,
    string SamplesDir)
{
    public static AppSettings Load(string[] args)
    {
        var envName = Environment.GetEnvironmentVariable("CROSSAPP_ENVIRONMENT") ?? "Development";

        // У командний рядок конфігурації йдуть лише ключі з ':' (--Storage:DataPath=tmp).
        // Власні прапорці (--file, --cache, --timeout=1000) конфігурацію не чіпають.
        var configArgs = args.Where(a => a.StartsWith("--", StringComparison.Ordinal) && a.Contains(':')).ToArray();

        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)   // не CurrentDirectory: запуск з будь-якого каталогу
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{envName}.json", optional: true)
            .AddUserSecrets(Assembly.GetExecutingAssembly(), optional: true)
            .AddEnvironmentVariables(prefix: "CROSSAPP_")
            .AddCommandLine(configArgs)
            .Build();

        var options = config.Get<AppOptions>() ?? new AppOptions();

        // Проста змінна CROSSAPP_DATA_PATH перекриває Storage:DataPath
        var storage = options.Storage.WithDataPath(Environment.GetEnvironmentVariable("CROSSAPP_DATA_PATH"));

        var baseDir = storage.Root == StorageRoot.LocalAppData
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CrossApp")
            : AppContext.BaseDirectory;

        return new AppSettings(
            config, options, storage, baseDir,
            DataDir: storage.ResolveDataDirectory(baseDir),
            DataFile: storage.ResolveFullPath(baseDir),
            LogDir: storage.ResolveLogDirectory(baseDir),
            SamplesDir: Path.Combine(AppContext.BaseDirectory, "data"));
    }

    // Секрет: друкуємо лише факт наявності, ніколи значення.
    public string ApiKeyStatus()
    {
        var apiKey = Config["Integration:ApiKey"];
        return string.IsNullOrEmpty(apiKey) ? "ApiKey: не заданий" : $"ApiKey: присутній, довжина {apiKey.Length}";
    }
}
