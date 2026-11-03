using Core.Domain;

namespace Core.Abstractions;

public interface IOrderStore
{
    IReadOnlyList<Product> List();    // IReadOnlyList: назовні не можна мутувати сховище в обхід Add/Update/Remove
    Product? GetById(string id);      // Product?: запису може не бути, це нормальний результат пошуку
    void Add(Product item);
    void Update(Product item);
    bool Remove(string id);           // bool: чи було що видаляти
}
