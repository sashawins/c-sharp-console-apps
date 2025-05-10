using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;

namespace ConsoleApp2
{
    /// <summary>
    /// Класс для хранения и управления результатами анализа текстового файла
    /// </summary>
    public class FileAnalysis
    {
        // Приватные поля для хранения данных
        private string fileName;        // Имя анализируемого файла
        private int wordCount;          // Счетчик слов в файле
        private int characterCount;     // Счетчик символов в файле
        private static readonly Mutex mutex = new Mutex(); // Мьютекс для синхронизации
        public string? ErrorMessage { get; set; }

        // Публичные свойства для доступа к данным
        public string FileName 
        { 
            get => fileName;
            private set => fileName = value;
        }
        public int WordCount => wordCount;           // Только для чтения
        public int CharacterCount => characterCount; // Только для чтения

        /// <summary>
        /// Конструктор класса, инициализирует объект с указанным именем файла
        /// </summary>
        /// <param name="fileName">Имя файла для анализа</param>
        public FileAnalysis(string fileName)
        {
            this.fileName = fileName;
            this.wordCount = 0;
            this.characterCount = 0;
        }

        /// <summary>
        /// Увеличивает счетчик слов на 1 с использованием мьютекса
        /// </summary>
        public void IncrementWordCount()
        {
            mutex.WaitOne();
            try
            {
                wordCount++;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Добавляет указанное количество символов к общему счетчику с использованием мьютекса
        /// </summary>
        /// <param name="count">Количество символов для добавления</param>
        public void AddCharacters(int count)
        {
            mutex.WaitOne();
            try
            {
                characterCount += count;
            }
            finally
            {
                mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Возвращает строковое представление результатов анализа файла
        /// </summary>
        public override string ToString()
        {
            return $"Файл: {fileName}\nКол-во слов: {wordCount:N0}\nКол-во символов: {characterCount:N0}";
        }
    }

    /// <summary>
    /// Основной класс приложения
    /// </summary>
    class Program
    {
        // Пул потоков для обработки файлов
        private static readonly ConcurrentDictionary<string, FileAnalysis> fileAnalyses = new();

        /// <summary>
        /// Асинхронно анализирует содержимое файла
        /// </summary>
        /// <param name="filePath">Путь к файлу</param>
        private static async Task AnalyzeFileAsync(string filePath)
        {
            try
            {
                var analysis = new FileAnalysis(Path.GetFileName(filePath));
                fileAnalyses[filePath] = analysis;

                // Асинхронное чтение файла
                string content;
                try
                {
                    content = await File.ReadAllTextAsync(filePath);
                }
                catch (FileNotFoundException)
                {
                    throw new Exception($"Файл не найден: {filePath}");
                }
                catch (DirectoryNotFoundException)
                {
                    throw new Exception($"Директория не найдена: {Path.GetDirectoryName(filePath)}");
                }
                catch (UnauthorizedAccessException)
                {
                    throw new Exception($"Нет доступа к файлу: {filePath}");
                }
                catch (IOException ex)
                {
                    throw new Exception($"Ошибка ввода-вывода при чтении файла {filePath}: {ex.Message}");
                }

                // Подсчет символов
                analysis.AddCharacters(content.Length);
                
                // Подсчет слов (разделенных пробелами)
                string[] words = content.Split(new[] { ' ', '\n', '\r', '\t' }, 
                    StringSplitOptions.RemoveEmptyEntries);
                foreach (var _ in words)
                {
                    analysis.IncrementWordCount();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке файла {filePath}: {ex.Message}");
                // Добавляем информацию об ошибке в результаты
                fileAnalyses[filePath] = new FileAnalysis(Path.GetFileName(filePath))
                {
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Точка входа в приложение
        /// </summary>
        static async Task Main(string[] args)
        {
            Console.WriteLine("\nДобро пожаловать в приложение для анализа файлов!\n");
            Console.WriteLine("------------------------");

            // Запрос путей к файлам
            Console.WriteLine("Введите пути к файлам для анализа (по одному на строку):");
            Console.WriteLine("Для завершения ввода введите пустую строку");

            var filePaths = new List<string>();
            string? input;
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
            {
                if (File.Exists(input))
                {
                    filePaths.Add(input);
                }
                else
                {
                    Console.WriteLine($"Файл не найден: {input}");
                }
            }

            if (filePaths.Count == 0)
            {
                Console.WriteLine("Не указано ни одного файла для анализа.");
                return;
            }

            Console.WriteLine($"\nНачинаем анализ {filePaths.Count} файлов...\n");

            // Создание и запуск асинхронных задач для каждого файла
            var tasks = filePaths.Select(filePath => AnalyzeFileAsync(filePath)).ToArray();

            // Ожидание завершения всех задач
            await Task.WhenAll(tasks);

            // Вывод результатов
            Console.WriteLine("\nРезультаты анализа:");
            Console.WriteLine("====================");

            var successfulAnalyses = fileAnalyses.Values.Where(a => string.IsNullOrEmpty(a.ErrorMessage));
            var failedAnalyses = fileAnalyses.Values.Where(a => !string.IsNullOrEmpty(a.ErrorMessage));

            foreach (var analysis in fileAnalyses.Values.Reverse())
            {
                if (!string.IsNullOrEmpty(analysis.ErrorMessage))
                {
                    Console.WriteLine($"\nФайл: {analysis.FileName}");
                    Console.WriteLine($"Ошибка: {analysis.ErrorMessage}");
                }
                else
                {
                    Console.WriteLine($"\n{analysis}");
                }
                Console.WriteLine("------------------------");
            }

            // Вывод общей статистики с использованием LINQ
            Console.WriteLine("\nОбщая статистика:");
            Console.WriteLine("=================");
            Console.WriteLine($"Всего файлов: {filePaths.Count}");
            Console.WriteLine($"Успешно обработано: {successfulAnalyses.Count()}");
            Console.WriteLine($"Ошибок обработки: {failedAnalyses.Count()}");
            Console.WriteLine($"Общее количество слов: {successfulAnalyses.Sum(a => a.WordCount):N0}");
            Console.WriteLine($"Общее количество символов: {successfulAnalyses.Sum(a => a.CharacterCount):N0}");

            Console.WriteLine("\nНажмите любую клавишу для выхода...\n");
            Console.ReadKey();
        }
    }
}
