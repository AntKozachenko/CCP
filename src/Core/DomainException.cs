namespace Core;

// Порушення бізнес-правила (наприклад, "замовлення вже підтверджено").
// Некоректні аргументи лишаються ArgumentException, технічні збої інфраструктури це StorageException.
public sealed class DomainException : Exception
{
    public DomainException(string message) : base(message) { }
    public DomainException(string message, Exception inner) : base(message, inner) { }
}
