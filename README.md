# CrossApp

Наскрізний проєкт з крос-платформного програмування.
Предметна область: **Замовлення** (клієнти, товари, замовлення, рядки замовлення), документація замовлень в інтернет-магазині.

Середовище: .NET SDK 10.0, Arch Linux x64.

## Структура

```
src/Core         доменна логіка без консолі (Domain, Services, Storage, Reports, Events, Import, Dto)
src/Cli          консольний застосунок і composition root (Program.cs)
tests/Core.Tests unit-тести (xUnit), посилаються лише на Core
data/            демонстраційні файли
```

Запускати з кореня репозиторію (шляхи до `data/` і `logs/` відносні до нього).

```
dotnet build
dotnet test
dotnet run --project src/Cli -- <команда> [прапорці]
```

## Домен (лаб. 4)

- `Order`, `OrderLine`, `Product` створюються лише фабриками `Create` / `FromDto`, стан змінюється лише методами.
- Інваріанти: id замовлення й клієнта непорожні; кількість у рядку > 0; ціна рядка ≥ 0; ціна товару > 0; рядки додаються лише в `Draft`; порожнє замовлення не підтвердити; переходи `Draft → Confirmed | Cancelled`, `Confirmed → Cancelled`; клієнт має не більше 3 чернеток.

## Сховища й сервіс (лаб. 5)

`OrderService` залежить від `IOrderStore`; реалізація обирається в `Program.cs` (ручний DI).

| Прапорець | Сховище |
| :--- | :--- |
| (немає) | `InMemoryOrderStore` з `SampleData` |
| `--file` | `FileOrderStore` (`data/catalog.json`) |
| `--cache` | декоратор `CachingOrderStore` поверх обраного |

## Події (лаб. 6)

`OrderService.Changed` (`Added`, `PriceChanged`, `Removed`) піднімається **лише після успішної зміни**, рівно один раз на операцію. Демо: `events`.

## Звіти (лаб. 7)

`ReportService` приймає `IEnumerable<Order>`, повертає `IReadOnlyList<DTO>`, рахує за підтвердженими замовленнями, топ-N детермінований (`ThenBy`).

```
report-top | report-big | report-groups | report-sum
report-products | report-unsold | report-orders | report-groups-q | report-defer
report-bench   (запускати з -c Release)
```

Прапорці: `--empty`, `--shuffle`.

## Async, скасування, винятки, тести (лаб. 8)

### Типи винятків

| Тип | Коли |
| :--- | :--- |
| `DomainException` | порушено бізнес-правило: «замовлення вже підтверджено», рядок у не-`Draft`, порожнє замовлення, поганий запис у файлі імпорту |
| `ArgumentException` / `ArgumentOutOfRangeException` | некоректний аргумент методу (кількість ≤ 0, порожня назва) |
| `StorageException` | технічна помилка файлу (пошкоджений JSON); `InnerException` зберігає `JsonException` |
| `OperationCanceledException` | очікуване скасування, не збій; проходить наскрізь без обгортання |

### Імпорт

| Що | Де |
| :--- | :--- |
| `ICatalogImporter`, `JsonCatalogImporter` (`LoadAsync`, `StreamAsync`) | `Core/Abstractions`, `Core/Storage` |
| `OrderService.ImportAsync` / `ImportWithTimeoutAsync` / `ImportStreamAsync` | `Core/Services` |
| `IOrderImporter`, `JsonOrderImporter` (замовлення з рядками) | `Core/Abstractions`, `Core/Storage` |
| `import`, `import-orders`, `ConsoleProgress` | `Cli` |

Файли читаються через `FileStream(useAsync: true)`, токен передається ланцюжком `Cli → ImportAsync → LoadAsync` і перевіряється в циклах. Сховище лишилось синхронним (працює в пам'яті або з невеликим файлом).

Скасування не відкочує вже додані записи. Потоковий імпорт (`--stream`) теж не атомарний: записи до поганого вже додані.

### Команди

```
dotnet run --project src/Cli -- import                       # data/products.json, 17 товарів
dotnet run --project src/Cli -- import --stream              # потоковий імпорт
dotnet run --project src/Cli -- import --progress            # відсотки
dotnet run --project src/Cli -- import data/sample.json      # поганий запис: «Дані відхилено: Запис 4: ...»
dotnet run --project src/Cli -- import-orders                # data/orders.json
```

Коди завершення: `0` успіх, `1` помилка даних чи файлу, `2` скасовано або таймаут.

### Як перевірити скасування

`--slow=<мс>` додає підписнику події затримку, щоб імпорт тривав кілька секунд.

```
dotnet run --project src/Cli -- import --slow=300 --progress                  # натисніть Ctrl+C
dotnet run --project src/Cli -- import --slow=300 --progress --timeout=1000   # таймаут
```

Ctrl+C дає `Імпорт скасовано.`, таймаут дає `Імпорт перервано: ...`.

### Тести

```
dotnet test
dotnet test --collect:"XPlat Code Coverage"
```

Тести працюють із `InMemoryOrderStore` і підставними імпортерами. Диск використовують лише тести `JsonCatalogImporter` / `JsonOrderImporter`: тимчасовий файл у `Path.GetTempPath()`, що видаляється в `Dispose` (`TempFile`). Каталог `data/` тести не читають.
