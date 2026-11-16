namespace Core;

// Технічна помилка інфраструктури (пошкоджений файл, збій серіалізації).
// Не доменна: користувач нічого не порушив. Початковий виняток обов'язково зберігається в InnerException.
public sealed class StorageException(string message, Exception inner) : Exception(message, inner);
