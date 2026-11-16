namespace Cli;

// Додаткове 3 (лаб. 8): підписник IProgress<int>, що малює відсотки в одному рядку.
// Власна синхронна реалізація, а не Progress<T>: той викликає обробник через пул потоків,
// і відсотки могли б з'явитися вже після фінального повідомлення.
public sealed class ConsoleProgress : IProgress<int>
{
    private bool _drawn;

    public void Report(int percent)
    {
        Console.Write($"\r Імпорт: {percent,3}%");
        _drawn = true;
    }

    // Завершує рядок з відсотками, щоб наступне повідомлення не склеїлось із ним.
    public void EndLine()
    {
        if (!_drawn) return;
        Console.WriteLine();
        _drawn = false;
    }
}
