using Core.Domain;

namespace Core.Abstractions;

// Контракт сховища каталогу товарів. Лише те, що справді потрібно OrderService.
// Жодних SaveToFile/Flush: це деталі файлової реалізації, а не доменні операції.
public interface IOrderStore
{
    IReadOnlyList<Product> List();    // IReadOnlyList: назовні не можна мутувати сховище в обхід Add/Update/Remove
    Product? GetById(string id);      // Product?: запису може не бути, це нормальний результат пошуку
    void Add(Product item);
    void Update(Product item);
    bool Remove(string id);           // bool: чи було що видаляти
}
