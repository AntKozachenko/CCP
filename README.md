# CrossApp
 Наскрізний проєкт з крос-платформного програмування.
Предметна область: Замовлення (клієнти, товари, замовлення, рядок замовлення)
 Призначення: документація замовлень в (інтернет-)магазині.
 ## Запуск
```
 dotnet build


dotnet run --project src/Cli
```
 ## Середовище
 .NET SDK 10.0, Arch Linux x64

# Розмір прогами

| RID    | Режим | Розмір publish|
| :-------|--------|:---------:|
|win-x64| Self-contained | 78MБ|
|win-x64| framework-dependent | 0.216МБ|
|win-x64| Single File | 73.6МБ|
|win-x64| Trimmed| 20.3МБ|
|linux-x64  |Self-contained       | 80МБ|
|linux-x64|framework-dependent| 0.136МБ|

## Імпорт даних (лаб. 3)

Cli читає товари з CSV-файлу. За замовчуванням це `data/sample.csv`, інший шлях передається першим аргументом:

```
dotnet run --project src/Cli
dotnet run --project src/Cli -- шлях/до/файлу.csv
```

Запускати з кореня репозиторію, бо шлях за замовчуванням відносний.

### Формат файлу

- Кодування: UTF-8
- Роздільник: `;` (задано константою `Separator` в `ProductCsvImporter`)
- Перший рядок: заголовок `id;name;price` (необов'язковий, пропускається, якщо починається з `id`)
- Порожні рядки та рядки, що починаються з `#`, ігноруються
- Колонки: `id` (рядок, наприклад `P-001`), `name` (непорожня назва), `price` (невід'ємне число з крапкою як десятковим роздільником, розбір через `InvariantCulture`; `12,50` вважається помилкою)
- Пошкоджені рядки не переривають імпорт: вони пропускаються й виводяться в кінці з номером рядка. У `data/sample.csv` 3 такі рядки залишено навмисно як тестові дані

### Код завершення

`0` за успіху, `1`, якщо файл не знайдено.

## Доменна модель (лаб. 4)

Сутності в `Core/Domain`: `Order` (замовлення), `OrderLine` (рядок замовлення), `Product` (товар каталогу).
Records з `Core/Dto` лишаються форматом даних (DTO), а правила живуть у сутностях.
Створити сутність можна лише через фабричний метод `Create` або `FromDto`, змінити стан лише через методи.

### Інваріанти

- Ідентифікатор замовлення та клієнта не порожні (`Order.Create`)
- Ідентифікатор товару в рядку не порожній (`OrderLine.Create`)
- Кількість у рядку більша за нуль (`OrderLine.Create`)
- Ціна в рядку не від'ємна (`OrderLine.Create`)
- Рядки можна додавати лише в замовлення зі статусом `Draft` (`Order.AddLine`)
- Порожнє замовлення не можна підтвердити (`Order.Confirm`)
- Допустимі переходи статусу: `Draft → Confirmed`, `Draft → Cancelled`, `Confirmed → Cancelled`; `Cancelled` кінцевий (`Order.MoveTo`)
- `FromDto` відновлює сутність через ті самі методи, тому пошкоджені дані відхиляються
- Ціна товару в каталозі більша за нуль (`Product.Create`)
- Клієнт не може мати більше 3 чернеток одночасно (`OrderPlacement.StartOrder`)

## Сервісний шар і сховища (лаб. 5)

Доменна логіка не залежить від способу зберігання даних: сервіс працює з інтерфейсом сховища, а конкретна реалізація обирається в одному місці, у `Program.cs` (composition root). DI-контейнер (`Microsoft.Extensions.DependencyInjection`) навмисно не підключено: залежності передаються через конструктор вручну.

| Роль | Тип | Розташування |
| :--- | :--- | :--- |
| Інтерфейс сховища | `ICatalogStore` | `Core/Abstractions` |
| Реалізація в пам'яті | `InMemoryCatalogStore` | `Core/Storage` |
| Реалізація у файлі | `FileCatalogStore` | `Core/Storage` |
| Декоратор з кешем (додаткове) | `CachingCatalogStore` | `Core/Storage` |
| Сервіс | `CatalogService` | `Core/Services` |
| Демо-дані (16 товарів) | `SampleData` | `Core` |
| Фабрика сховищ (додаткове) | `StoreFactory` | `Cli` |

### Схема залежностей

```
Cli (Program.cs) → CatalogService → ICatalogStore ← InMemoryCatalogStore | FileCatalogStore | CachingCatalogStore
```

Конкретні класи сховищ створюються лише в `Program.cs` (і в `StoreFactory`, якщо використовувати фабрику). У `Core/Services` немає жодної згадки про `FileCatalogStore`, а в `Core` немає виводу в консоль.

### Контракт `ICatalogStore`

| Метод | Призначення |
| :--- | :--- |
| `List()` | усі товари; повертає `IReadOnlyList`, щоб зовнішній код не змінював сховище в обхід `Add`/`Update`/`Remove` |
| `GetById(id)` | товар або `null`; відсутність запису це нормальний результат пошуку |
| `Add(item)` | додає товар; дубль id дає `InvalidOperationException` |
| `Update(item)` | замінює товар з тим самим id; якщо такого немає, `InvalidOperationException` |
| `Remove(id)` | видаляє товар; повертає `true`, якщо було що видаляти |

У контракті лише доменні операції. Деталі файлу (`Flush`, шлях) живуть тільки в `FileCatalogStore`.

### Реалізації

- `InMemoryCatalogStore`: словник у пам'яті, початкові дані передаються в конструктор (`seed`). Після завершення програми дані зникають.
- `FileCatalogStore`: JSON-файл. Дані читаються один раз при першому зверненні й тримаються в кеші, після кожної зміни файл переписується. На диск потрапляють `ProductDto`, а не сутності: формат файлу не диктує форму домену, а `Product.FromDto` проганяє дані через інваріанти.
- `CachingCatalogStore`: декоратор. Приймає будь-яке інше `ICatalogStore`, кешує читання (`List`, `GetById`), а будь-яка зміна скидає кеш.

Унікальність id перевіряє сховище (це правило про набір записів), решту інваріантів перевіряє `Product.Create`.

### Операції `CatalogService`

| Операція | Опис |
| :--- | :--- |
| `Add(name, price)` | створює товар з новим id через `Product.Create` і додає в сховище |
| `ChangePrice(id, newPrice)` | `Product` незмінний, тому створюється новий товар з тим самим id і замінює старий |
| `Remove(id)` | видаляє товар; неіснуючий id дає `InvalidOperationException` |
| `Find(id)` | товар або `null` |
| `All()` | усі товари |
| `Search(predicate)` | товари за умовою `Func<Product, bool>` (додаткове) |

### Запуск

Режим вибирається аргументом Cli:

```
dotnet run --project src/Cli                  # InMemoryCatalogStore, стартові дані з SampleData, після виходу зникають
dotnet run --project src/Cli -- --file        # FileCatalogStore, дані накопичуються між запусками
dotnet run --project src/Cli -- --file --cache  # те саме з декоратором CachingCatalogStore
```

Файл даних `catalog.json` створюється в підкаталозі `data` поруч зі збіркою (`AppContext.BaseDirectory`), тобто всередині `bin/`. Команда `dotnet clean` його видаляє. Кирилиця в файлі записана як `\uXXXX`: це штатне екранування `System.Text.Json`.

Сценарій у `Program.cs`: додати товар, змінити ціну, показати список, знайти за id, пошук за умовою, видалити тимчасовий товар, а потім п'ять відмов (дубль id, неіснуючий id для `ChangePrice` і `Remove`, нульова ціна, порожня назва) з зрозумілими повідомленнями.

### Додаткові завдання

1. `CachingCatalogStore`: декоратор з тим самим контрактом. Вмикається прапорцем `--cache`, у кінці виводу показує кількість влучань і промахів кешу.
2. `CatalogService.Search(Func<Product, bool>)`: пошук за довільною умовою. Метод у сервісі, а не в інтерфейсі, щоб контракт лишався мінімальним.
3. `StoreFactory.Create(args)`: вибір сховища з аргументів винесено з `Program.cs`. Це частина composition root, а не відхід від нього: конкретні класи знає лише точка входу. Щоб скористатися фабрикою, у `Program.cs` розкоментований рядок з `Cli.StoreFactory.Create(args)` замінює тернарний блок.

### Що зміниться, коли з'явиться база даних

Додасться ще один клас, що реалізує `ICatalogStore` (наприклад, на Entity Framework Core). `CatalogService`, `Product` і DTO не зміняться, у `Program.cs` зміниться лише рядок створення сховища.

## Доменні події, делегати, лямбди (лаб. 6)

Core повідомляє, що сталося, а що з цим робити, вирішує зовнішній код. У `Core` немає ні `Console`, ні роботи з файлом журналу.

### Які зміни вважаються подіями

| Вид (`CatalogChangeKind`) | Коли піднімається | Дані в `CatalogChangedEventArgs` |
| :--- | :--- | :--- |
| `Added` | після `OrderService.Add` | `Id`, `NewPrice` |
| `PriceChanged` | після `OrderService.ChangePrice` | `Id`, `OldPrice`, `NewPrice` |
| `Removed` | після `OrderService.Remove` | `Id`, `OldPrice` |

Подія піднімається **лише після успішної зміни**: якщо `Product.Create` або сховище кинули виняток, повідомлення немає. Усі події мають `At` (момент створення аргументів).

### Склад

| Що | Де |
| :--- | :--- |
| `CatalogChangeKind`, `CatalogChangedEventArgs` | `Core/Events` |
| `event EventHandler<CatalogChangedEventArgs>? Changed`, `OnChanged` | `Core/Services/OrderService.cs` |
| `INotificationSink` (додаткове 1) | `Core/Abstractions` |
| `ConsoleSink`, `FileSink`, `ChangeFormatter`, `RepoPaths` | `Cli` |
| `ProductCsvImporter.Load(path, Action<string>? onSkipped)` (додаткове 2) | `Core/Import` |

### Підписники

Зараз: `OnCatalogChanged` (локальна функція в `Program.cs`: консоль + `logs/app.log`) і лямбда-лічильник `(_, _) => changes++` (замикання над `changes`). Обробники викликаються по черзі в порядку підписки. На тижні 10 підписником стане логер Api, на тижні 12 компонент Blazor, який оновлює таблицю.

### Де використано Func / Action

- `OrderService.Search(Func<Product, bool>)`: потрібна відповідь, тому делегат, а не подія.
- `ProductCsvImporter.Load(path, Action<string>? onSkipped)`: викликач сам вирішує, що робити з повідомленням про пропущений рядок.

### Подія чи INotificationSink (додаткове 1)

| | `event` | `INotificationSink` |
| :--- | :--- | :--- |
| Хто підключає | будь-хто після створення сервісу (`+=` / `-=`) | composition root, один раз у конструкторі |
| Відписка під час роботи | є (`-=`) | немає (список фіксований) |
| Контракт | сигнатура делегата | інтерфейс, який зручно підмінити в тесті |
| Наслідок | забутий `-=` тримає підписника в пам'яті | приймач живе стільки ж, скільки сервіс |

### Інваріанти подій

1. Кожна успішна операція дає рівно одну подію.
2. Операція, що впала на інваріанті, подій не дає (у `Program.cs` це перевіряється лічильником: «Подій за час відмов: 0»).
3. Після `-=` обробник мовчить, але дані змінюються.
4. Подія оголошена як `event`, а не як публічне поле делегата.
5. Помилка у підписнику ламає бізнес-операцію (необроблений виняток зупиняє наступних підписників), тому ризиковану роботу в обробнику слід обгортати в `try/catch`.

### Запуск

```
dotnet run --project src/Cli
dotnet run --project src/Cli -- --file --cache
```

`logs/app.log` і `logs/sinks.log` створюються кодом (`Directory.CreateDirectory`) у корені репозиторію й перезаписуються на кожному запуску демо.

## Звіти через LINQ (лаб. 7)

Усі запити живуть у `Core/Reports/ReportService.cs`. Вони приймають `IEnumerable<Order>` (а не сховище) і повертають `IReadOnlyList<DTO>`. `Program.cs` не містить жодного `Where`/`GroupBy`: лише виклики `ReportService` і вивід через `ReportPrinter`. Лише LINQ to Objects, без PLINQ і запитів до БД.

Правило звітів: «клієнт», «чек» і «товар» рахуються за **підтвердженими** замовленнями (`Confirmed`). Чернетка ще не продаж, скасоване не продаж. Правило записане один раз у приватному `Confirmed(...)`.

### Перелік запитів

| Метод | Питання, на яке відповідає | Оператори |
| :--- | :--- | :--- |
| `ExpensiveOrders(source, threshold)` | Які замовлення дорожчі за поріг? | `Where`, `OrderByDescending`, `ThenBy`, `Select` |
| `TopCustomers(source, n)` | Які n клієнтів принесли найбільше? | `Where`, `GroupBy`, `Select`, `OrderByDescending`, `ThenBy`, `Take` |
| `ByStatus(source)` | Як замовлення розподілені за статусом? | `GroupBy`, `Select`, `OrderByDescending`, `ThenBy` |
| `Totals(source)` | Який середній чек і найбільше замовлення? | `Where`, `Select`, `Sum`, `Average`, `Max` |
| `TopProducts(orders, catalog, n)` (дод.) | Які товари дали найбільший виторг? | `SelectMany`, `Join`, `GroupBy`, власний `TopBy` |
| `UnsoldProducts(orders, catalog)` (дод.) | Які товари ніхто не купив? | `GroupJoin`, `Where`, `OrderBy` |
| `TopOrders(source, n)` (дод.) | Які n найбільших підтверджених замовлень? | `Where`, `Select`, власний `TopBy` |
| `ByStatusQuerySyntax(source)` (дод.) | Те саме, що `ByStatus` | `from … group … into … let … orderby … select` |

### Фрагмент GroupBy з проєкцією

```csharp
Confirmed(source)
    .GroupBy(o => o.CustomerId, StringComparer.Ordinal)                      // g.Key: id клієнта, g: його замовлення
    .Select(g => new CustomerSummary(g.Key, g.Count(), g.Sum(o => o.Total))) // g.Count(): скільки замовлень у групі
    .OrderByDescending(c => c.TotalAmount).ThenBy(c => c.CustomerId, StringComparer.Ordinal)
    .Take(n)
    .ToList();
```

`Select` отримує групу, а не `Order`: `g.Key` це значення ключа групи, `g.Count()` і `g.Sum(...)` рахують лише в межах цієї групи. Після `Select` кожен елемент уже `CustomerSummary`, тому наступне сортування працює з готовими підсумками.

### Підкоманди Cli

```
dotnet run --project src/Cli -- report-top        (за замовчуванням)
dotnet run --project src/Cli -- report-big
dotnet run --project src/Cli -- report-groups
dotnet run --project src/Cli -- report-sum
dotnet run --project src/Cli -- report-products | report-unsold | report-orders
dotnet run --project src/Cli -- report-groups-q   (синтаксис запитів + перевірка збігу)
dotnet run --project src/Cli -- report-defer      (демо відкладеного виконання)
dotnet run -c Release --project src/Cli -- report-bench   (Stopwatch, 100 000 замовлень)
dotnet run --project src/Cli -- events            (демо лаб. 6)
```

Прапорці: `--file`, `--cache` (сховище, лаб. 5), `--empty` (порожнє джерело), `--shuffle` (інший порядок у джерелі). Прапорець не вважається назвою звіту: команда береться як перший аргумент без `--`.

### Коли виконується запит і чому два Count() дали різні числа

Запит `source.Where(o => o.Total > threshold)` лише **описує** вибірку: у момент оголошення жодне замовлення не перевірено. Виконання починається при кожному `Count()` (і при `foreach`/`ToList()`), причому щоразу заново проходить поточний вміст `source`. Тому перший `Count()` дав 4, а після `source.Add(...)` другий `Count()` того самого запиту дав 5: джерело встигло змінитись між двома виконаннями. `ToList()` фіксує результат: після `source.Clear()` у знімку лишилось 5, а сам запит дав 0. Лічильник предиката в замиканні підтверджує: два `Count()` викликали предикат 36 разів (18 × 2), а після одного `ToList()` читання `list.Count` предикат не викликає взагалі (18 разів загалом).

### Відповіді на типові питання

- **Відкладені**: `Where`, `Select`, `OrderBy`, `GroupBy`, `Join`, `Take` (запит лише будується). **Негайні**: `ToList`, `ToArray`, `Count()`, `Sum`, `Average`, `Max`, `First`, `foreach`.
- **Потокові** (`Where`, `Select`, `Take`) передають елементи по одному. **Буферизувальні** (`OrderBy`, `GroupBy`, `Join` для внутрішньої послідовності, `GroupJoin`) мусять спершу прочитати все й тримати в пам'яті. Тому `Where` ставимо перед `OrderBy`: сортуємо лише відібране.
- **Порожня послідовність**: `Sum` і `Count()` дають 0, а `Average`/`Max` для типів без `null` кидають `InvalidOperationException`. У `Totals` порожнечу перевіряємо до виклику (`Count == 0`) і повертаємо нулі.
- **Гроші** рахуємо в `decimal`, тому і `Average` у `Totals` це `decimal`, а не `double`.

### Додаткові завдання

1. **Join / GroupJoin.** `TopProducts`: `Join` рядків замовлень із каталогом (потрібна назва товару, її в рядку немає). `UnsoldProducts`: `GroupJoin` лишає товар у результаті, навіть якщо відповідних рядків немає, тому знаходить товари без продажів (`P-006`, `P-016`).
2. **Власний метод-розширення** `TopBy<T, TKey>(key, tieBreaker, n)` у `EnumerableExtensions`. Другий ключ обов'язковий параметр, тож забути його неможливо. Використано у двох звітах: `TopProducts` і `TopOrders`. Метод навмисно не iterator (без `yield`), щоб перевірка аргументів спрацьовувала одразу. `TopCustomers` залишено з явним `OrderByDescending/ThenBy`, щоб патерн було видно без виклику допоміжного методу.
3. **Синтаксис запитів.** `ByStatusQuerySyntax` дає той самий результат (`report-groups-q` перевіряє `SequenceEqual`). Порівняння читабельності: запис `from … group … into g … let total = … orderby … select` зручний, коли в центрі групування і треба зберегти проміжне значення (`let`): суму групи не рахуємо двічі. У методному записі для цього потрібен проміжний `Select` у кортеж. Мінуси синтаксису запитів: у `group by` не можна передати `StringComparer`, а `Take`, `First`, `Join` з компаратором все одно потребують методного виклику, тож ланцюжок «Where → ThenBy → Take» читається краще саме методами.
4. **Stopwatch на 100 000 замовлень** (`report-bench`, медіана з 9 прогонів, Release, розігрів і `GC.Collect` перед кожним виміром). Приклад результатів на моїй машині (ваші цифри будуть іншими, перезапустіть і підставте свої):

   | Варіант | Час, мс |
   | :--- | ---: |
   | Топ-5 клієнтів: без проміжного `ToList` | 31–39 |
   | Топ-5 клієнтів: `ToList` після `Where` | 32–37 |
   | Топ-5 клієнтів: `ToList` після кожного кроку | 37–38 |
   | Дорожчі за 5000: запит двічі (`Count()` + `ToList()`) | 45–60 |
   | Дорожчі за 5000: `ToList` один раз | 24–32 |

   Висновок: `ToList` посередині ланцюжка швидкості не додає (різниця в межах шуму), бо дані й так проходять усі кроки один раз, а проміжні списки лише виділяють пам'ять. Матеріалізація потрібна, коли результат використовується **кілька разів**: подвійне перебирання запиту стабільно в 1,5–2 рази повільніше, бо `Total` (сума рядків) обчислюється двічі для кожного замовлення.
