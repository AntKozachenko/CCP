namespace Core.Events;

// Що саме сталося з каталогом товарів. Подія піднімається лише після УСПІШНОЇ зміни.
public enum CatalogChangeKind { Added, PriceChanged, Removed }
