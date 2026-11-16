using System.Text.Json;

namespace Core.Storage;

// Спільне для JSON-імпортерів: один спосіб відкрити файл і одні налаштування серіалізації.
internal static class JsonFile
{
    public static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    // useAsync: true дає справжній асинхронний ввід-вивід, а не синхронне читання в потоці з пулу.
    public static FileStream OpenRead(string path) =>
        new(path, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);

    // Повідомлення ArgumentOutOfRangeException містить перенесення рядка: для однорядкового виводу прибираємо.
    public static string OneLine(Exception ex) => ex.Message.ReplaceLineEndings(" ");
}
