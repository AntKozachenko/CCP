namespace Cli;

// Шляхи відносно кореня репозиторію (теки з файлом рішення *.sln / *.slnx), а не папки збірки.
// Раніше FindDataPath дублювався в Program.cs і StoreFactory; тепер логіка одна.
public static class RepoPaths
{
    public static string Root { get; } = FindRoot();

    public static string Combine(params string[] parts) => Path.Combine([Root, .. parts]);

    // Іде вгору від папки збірки; якщо рішення не знайдено (запуск поза репозиторієм),
    // падає назад на папку збірки.
    private static string FindRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !dir.EnumerateFiles("*.sln*").Any())
            dir = dir.Parent;
        return dir?.FullName ?? AppContext.BaseDirectory;
    }
}
