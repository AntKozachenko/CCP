using System.Text.Encodings.Web;
using System.Text.Json;
using Core.Domain;
using Core.Dto;

namespace Core.Storage;

// Лаб. 9. Єдине місце, що знає формат файлу колекції: ProductDto у JSON.
// Спільне для FileOrderStore і команди export, щоб формат не розходився.
public static class JsonCatalogFile
{
    // camelCase у файлі; кирилиця читабельна (не \u0426); регістр при читанні не важливий,
    // тож старі файли з "Id"/"Name"/"Price" теж читаються.
    // UnsafeRelaxedJsonEscaping: файл не вбудовується в HTML, тому тут це безпечно.
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Зіпсований файл → StorageException з назвою файлу, а не необроблений stack trace.
    public static IReadOnlyList<Product> Read(string path)
    {
        var name = Path.GetFileName(path);
        List<ProductDto?> dtos;
        try
        {
            dtos = JsonSerializer.Deserialize<List<ProductDto?>>(File.ReadAllText(path), Options) ?? [];
        }
        catch (JsonException ex)
        {
            throw new StorageException($"Файл {name} пошкоджено: {ex.Message.ReplaceLineEndings(" ")}", ex);
        }

        var result = new List<Product>(dtos.Count);
        for (var i = 0; i < dtos.Count; i++)
        {
            try
            {
                // FromDto іде через Product.Create, тож зіпсований запис відхиляється інваріантами
                if (dtos[i] is null) throw new ArgumentException("порожній елемент");
                result.Add(Product.FromDto(dtos[i]!));
            }
            catch (ArgumentException ex)   // ловить і ArgumentOutOfRangeException
            {
                throw new StorageException(
                    $"Файл {name}, запис {i + 1}: {ex.Message.ReplaceLineEndings(" ")}", ex);
            }
        }
        return result;
    }

    // Атомарний запис: спершу повний файл поруч (.tmp), потім File.Move з overwrite.
    // Якщо процес впаде посеред запису, цільовий файл лишається цілим (старим), а не обірваним.
    public static void Write(string path, IEnumerable<Product> products)
    {
        ArgumentNullException.ThrowIfNull(products);
        var full = Path.GetFullPath(path);
        var tmp = full + ".tmp";

        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        try
        {
            var dtos = products.Select(p => p.ToDto()).ToList();
            File.WriteAllText(tmp, JsonSerializer.Serialize(dtos, Options));   // UTF-8 без BOM
            File.Move(tmp, full, overwrite: true);
        }
        catch
        {
            if (File.Exists(tmp)) File.Delete(tmp);
            throw;
        }
    }
}
