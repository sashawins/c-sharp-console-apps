using System;
using System.IO;

namespace ConsoleApp1
{
    /// <summary>
    /// Основной класс программы для анализа текстовых файлов
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// Точка входа в приложение
        /// </summary>
        /// <param name="args">Аргументы командной строки (не используются)</param>
        static void Main(string[] args)
        {
            // Приветственное сообщение
            Console.WriteLine("Добро пожаловать в приложение для обработки текстовых файлов!\n");

            // Запрос пути к файлу у пользователя
            Console.Write("Укажите путь к текстовому файлу: ");
            string? filePath = Console.ReadLine()?.Trim('"');

            // Использование FileOwner для безопасной работы с файлом
            using (var fileOwner = new FileOwner(filePath!))
            {
                // Проверка успешности открытия файла
                if (!fileOwner.IsValid)
                {
                    Console.WriteLine("Ошибка: не удалось обработать файл. Выход.");
                    return;
                }

                // Получение содержимого файла
                string fileContent = fileOwner.Content;
                Console.WriteLine($"Файл успешно прочитан! Размер содержимого: {fileContent.Length} символов.");
                Console.WriteLine();

                // Запрос ключевого слова для поиска
                Console.Write("Укажите слово для поиска: ");
                string? keyword = Console.ReadLine();

                // Проверка валидности ключевого слова
                if (string.IsNullOrEmpty(keyword))
                {
                    Console.WriteLine("Ошибка: слово для поиска не может быть пустым. Выход.");
                    return;
                }

                // Запуск анализа содержимого файла
                AnalyzeContent(fileContent, keyword);
            }

            /// <summary>
            /// Анализирует содержимое файла, подсчитывая общее количество слов и количество вхождений ключевого слова
            /// </summary>
            /// <param name="content">Содержимое файла для анализа</param>
            /// <param name="keyword">Ключевое слово для поиска</param>
            static void AnalyzeContent(in string content, in string keyword)
            {
                Console.WriteLine("Анализ содержимого . . . ");

                // Использование ReadOnlySpan для оптимизации работы со строками
                ReadOnlySpan<char> contentSpan = content.AsSpan();
                ReadOnlySpan<char> keywordSpan = keyword.AsSpan();

                int wordsCount = 0;
                int keywordCount = 0;
                bool inWord = false;

                // Пошаговый анализ текста
                for (int i = 0; i < contentSpan.Length; i++)
                {
                    // Проверка на разделитель слов
                    if (char.IsWhiteSpace(contentSpan[i]) || IsPunctuation(contentSpan[i]))
                    {
                        inWord = false;
                        continue;
                    }

                    // Обработка начала нового слова
                    if (!inWord)
                    {
                        wordsCount++;
                        inWord = true;

                        // Проверка на совпадение с ключевым словом
                        if (contentSpan.Slice(i, keywordSpan.Length).Equals(keywordSpan, StringComparison.OrdinalIgnoreCase))
                        {
                            keywordCount++;
                        }
                    }
                }
                // Вывод результатов анализа
                Console.WriteLine($"Общее количество слов: {wordsCount}.");
                Console.WriteLine($"Слово \"{keyword}\" встречается {keywordCount} раз(а).");
            }

            /// <summary>
            /// Проверяет, является ли символ знаком пунктуации
            /// </summary>
            /// <param name="c">Проверяемый символ</param>
            /// <returns>true если символ является знаком пунктуации, иначе false</returns>
            static bool IsPunctuation(char c)
            {
                return c == ' ' || c == '\n' || c == '\r' || c == '\t' || c == '.' || c == ',' || c == '-' || c == '!' || c == '?' || c == '_' || c == ':' || c == ';';
            }
        }
        
        /// <summary>
        /// Класс для безопасной работы с файлом, реализующий интерфейс IDisposable
        /// </summary>
        sealed class FileOwner : IDisposable
        {
            /// <summary>
            /// Содержимое файла
            /// </summary>
            public string Content { get; private set; }

            /// <summary>
            /// Флаг успешности открытия файла
            /// </summary>
            public bool IsValid { get; }

            /// <summary>
            /// Флаг, указывающий, что ресурсы были освобождены
            /// </summary>
            private bool _disposed;

            /// <summary>
            /// Конструктор класса FileOwner
            /// </summary>
            /// <param name="filePath">Путь к файлу для чтения</param>
            public FileOwner(string filePath)
            {
                // Проверка на пустой путь к файлу
                if (string.IsNullOrEmpty(filePath))
                {
                    Console.WriteLine("Ошибка: путь к файлу не может быть пустым. Выход.");
                    Content = string.Empty;
                    IsValid = false;
                    return;
                }

                try
                {
                    // Проверка существования файла
                    if (!File.Exists(filePath))
                    {
                        Console.WriteLine("Ошибка: файл не найден.");
                        Content = string.Empty;
                        IsValid = false;
                        return;
                    }

                    // Чтение содержимого файла
                    Content = File.ReadAllText(filePath);
                    IsValid = true;
                }
                catch (Exception ex)
                {
                    // Обработка ошибок при чтении файла
                    Console.WriteLine($"Ошибка чтения файла: {ex.Message}");
                    Content = string.Empty;
                    IsValid = false;
                }
            }

            /// <summary>
            /// Освобождает ресурсы, используемые классом FileOwner
            /// </summary>
            public void Dispose()
            {
                if (!_disposed)
                {
                    // Очищаем содержимое файла
                    Content = string.Empty;
                    _disposed = true;
                    
                    // Выводим сообщение об освобождении ресурсов
                    Console.WriteLine("Ресурсы файла были освобождены.");
                }
            }
        }
    }
}
