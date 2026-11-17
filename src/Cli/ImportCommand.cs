using Core;
using Core.Reports;
using Core.Services;
using Core.Storage;

namespace Cli;

// Лаб. 8. Cli створює CancellationTokenSource (джерело сигналу), Core лише перевіряє токен.
// Коди завершення: 0 успіх, 1 помилка даних або файлу, 2 скасовано чи вичерпано таймаут.
public static class ImportCommand
{
    public static Task<int> ProductsAsync(string[] args, OrderService service, AppSettings settings)
    {
        string path = PathArg(args, settings, "products.json");
        int? timeoutMs = IntOption(args, "--timeout");
        int slowMs = IntOption(args, "--slow") ?? 0;
        bool stream = args.Contains("--stream");
        var bar = args.Contains("--progress") ? new ConsoleProgress() : null;

        return GuardedAsync(path, bar, async ct =>
        {
            int events = 0;
            service.Changed += (_, _) =>
            {
                events++;
                if (slowMs > 0) Thread.Sleep(slowMs);   // демо: повільний підписник, щоб встигнути натиснути Ctrl+C
            };

            var importer = new JsonCatalogImporter();
            int added = stream ? await service.ImportStreamAsync(importer, path, ct)
                : timeoutMs is int ms
                    ? await service.ImportWithTimeoutAsync(importer, path, TimeSpan.FromMilliseconds(ms), ct, bar)
                    : await service.ImportAsync(importer, path, ct, bar);

            bar?.EndLine();
            Console.WriteLine($"Імпортовано записів: {added}");
            Console.WriteLine($"Подій Added отримано: {events}");
        });
    }

    public static Task<int> OrdersAsync(string[] args, AppSettings settings)
    {
        string path = PathArg(args, settings, "orders.json");

        return GuardedAsync(path, null, async ct =>
        {
            var orders = await new JsonOrderImporter().LoadAsync(path, ct);
            Console.WriteLine($"Імпортовано замовлень: {orders.Count}");
            ReportPrinter.Print("Підсумки по підтверджених замовленнях:", ReportService.Totals(orders));
        });
    }

    private static async Task<int> GuardedAsync(string path, ConsoleProgress? bar, Func<CancellationToken, Task> body)
    {
        using var cts = new CancellationTokenSource();
        ConsoleCancelEventHandler onCancel = (_, e) => { e.Cancel = true; cts.Cancel(); };
        Console.CancelKeyPress += onCancel;
        try
        {
            await body(cts.Token);
            return 0;
        }
        // Скасування це очікувана ситуація, а не збій: окремий catch, і до загальних обробників.
        catch (OperationCanceledException) { bar?.EndLine(); Console.WriteLine("Імпорт скасовано."); return 2; }
        catch (TimeoutException ex) { bar?.EndLine(); Console.WriteLine($"Імпорт перервано: {ex.Message}"); return 2; }
        catch (DomainException ex) { bar?.EndLine(); Console.WriteLine($"Дані відхилено: {ex.Message}"); return 1; }
        catch (StorageException ex) { bar?.EndLine(); Console.WriteLine($"Помилка сховища: {ex.Message}"); return 1; }
        catch (FileNotFoundException) { bar?.EndLine(); Console.WriteLine($"Файл не знайдено: {path}"); return 1; }
        catch (IOException ex) { bar?.EndLine(); Console.WriteLine($"Помилка введення-виведення: {ex.Message}"); return 1; }
        finally
        {
            Console.CancelKeyPress -= onCancel;   // після Dispose токена обробник Ctrl+C не повинен його чіпати
        }
    }

    // Другий позиційний аргумент (перший це команда); за замовчуванням демо-файл поруч зі збіркою (<вихід>/data).
    private static string PathArg(string[] args, AppSettings settings, string defaultFile) =>
        args.Where(a => !a.StartsWith("--")).Skip(1).FirstOrDefault() ?? Path.Combine(settings.SamplesDir, defaultFile);

    private static int? IntOption(string[] args, string name)
    {
        string? raw = args.FirstOrDefault(a => a.StartsWith(name + "=", StringComparison.Ordinal));
        return raw is not null && int.TryParse(raw[(name.Length + 1)..], out int value) ? value : null;
    }
}
