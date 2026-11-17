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

## Шляхи, JSON, конфігурація, secrets (лаб. 9)

Шляхи будуються через `Path.Combine` (роздільник підставляє ОС; конкатенація з `"\\"` ламається на Linux/macOS). Абсолютних літералів у `src` немає. Відносні шляхи рахуються від `AppContext.BaseDirectory` (а не `Environment.CurrentDirectory`), тому запуск з кореня solution і з `src/Cli` дає той самий файл.

### Порядок джерел (пізніше перекриває раніше)

`appsettings.json` → `appsettings.{Environment}.json` → user-secrets → змінні `CROSSAPP_*` → `--Ключ:Підключ=значення`. Додатково проста змінна `CROSSAPP_DATA_PATH` перекриває `Storage:DataPath`.

### Профілі

Профіль обирається змінною `CROSSAPP_ENVIRONMENT` (за замовчуванням `Development`).

| Профіль | Журнал | Дані |
| :--- | :--- | :--- |
| Development | `Debug` | `<вихід збірки>/data` |
| Production | `Warning` | `Environment.SpecialFolder.LocalApplicationData/CrossApp/data` (`Storage:Root=LocalAppData`) |

### Таблиця ключів

| Ключ | За замовчуванням | Змінна середовища |
| :--- | :--- | :--- |
| `Storage:DataPath` | `data` | `CROSSAPP_Storage__DataPath` або `CROSSAPP_DATA_PATH` |
| `Storage:FileName` | `catalog.json` | `CROSSAPP_Storage__FileName` |
| `Storage:LogPath` | `logs` | `CROSSAPP_Storage__LogPath` |
| `Storage:Root` | `AppBase` (`LocalAppData` у Production) | `CROSSAPP_Storage__Root` |
| `Logging:MinimumLevel` | `Information` | `CROSSAPP_Logging__MinimumLevel` |
| `Orders:Currency` | `UAH` | `CROSSAPP_Orders__Currency` |
| `EnvironmentName` | з профілю | `CROSSAPP_EnvironmentName` |
| `Integration:ApiKey` | лише user-secrets / env | `CROSSAPP_Integration__ApiKey` |

### Команди

```
dotnet run --project src/Cli -- config                 # шляхи й наявність секрету
dotnet run --project src/Cli -- export                 # уся колекція → <DataPath>/catalog.json (атомарно)
dotnet run --project src/Cli -- export my.json         # у вказаний файл
dotnet run --project src/Cli -- import my.json         # імпорт назад
dotnet run --project src/Cli -- config --Storage:DataPath=tmp
```

### Приклади змінних

```powershell
$env:CROSSAPP_DATA_PATH = "C:\Users\st\data"
$env:CROSSAPP_Storage__FileName = "items.json"
$env:CROSSAPP_Integration__ApiKey = "from-env"
$env:CROSSAPP_ENVIRONMENT = "Production"
```

```bash
CROSSAPP_DATA_PATH=/tmp/data dotnet run --project src/Cli -- export
CROSSAPP_Integration__ApiKey=from-env dotnet run --project src/Cli -- config
```

### Secret

```
dotnet user-secrets init --project src/Cli      # дописує UserSecretsId у Cli.csproj (GUID комітиться, ключ ні)
dotnet user-secrets set "Integration:ApiKey" "local-dev-key" --project src/Cli
dotnet user-secrets list --project src/Cli
```

Файл лежить поза репозиторієм: Windows `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json`, Linux/macOS `~/.microsoft/usersecrets/<UserSecretsId>/secrets.json`. Це лише для розробки (відкритий текст, профіль користувача); у продакшені використовуються змінні середовища або сховище secrets. Консоль друкує лише «присутній, довжина N».

### JSON

`JsonCatalogFile` — єдине місце формату: camelCase, `WriteIndented`, `UnsafeRelaxedJsonEscaping` (кирилиця без `\uXXXX`), атомарний запис `.tmp` + `File.Move(overwrite: true)`. Зіпсований файл дає `StorageException` з назвою файлу. Демо-вхідні файли з `data/` копіюються в `<вихід>/data` через `Cli.csproj`.
