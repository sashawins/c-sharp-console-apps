using System.Text;
using Microsoft.Extensions.Logging;

namespace ConsoleApp3;

/// <summary>
/// Класс, представляющий результаты анализа текстового файла
/// </summary>
public class FileAnalysis
{
    /// <summary>
    /// Количество строк в файле
    /// </summary>
    public int LineCount { get; set; }

    /// <summary>
    /// Количество слов в файле
    /// </summary>
    public int WordCount { get; set; }

    /// <summary>
    /// Количество символов в файле
    /// </summary>
    public int CharCount { get; set; }

    /// <summary>
    /// Имя проанализированного файла
    /// </summary>
    public string FileName { get; set; } = string.Empty;
}

/// <summary>
/// Класс для анализа текстовых файлов
/// </summary>
public class FileAnalyzer
{
    private readonly ILogger<FileAnalyzer> _logger;

    /// <summary>
    /// Создает новый экземпляр анализатора файлов
    /// </summary>
    /// <param name="logger">Логгер для записи информации об анализе</param>
    public FileAnalyzer(ILogger<FileAnalyzer> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Анализирует содержимое текстового файла
    /// </summary>
    /// <param name="filePath">Путь к файлу для анализа</param>
    /// <returns>Результаты анализа файла</returns>
    public async Task<FileAnalysis> AnalyzeFileAsync(string filePath)
    {
        try
        {
            // Читаем содержимое файла
            var content = await File.ReadAllTextAsync(filePath);
            
            // Создаем объект с результатами анализа
            var analysis = new FileAnalysis
            {
                FileName = Path.GetFileName(filePath),
                // Подсчитываем строки (учитываем последнюю строку, если файл не пустой)
                LineCount = content.Count(c => c == '\n') + (content.Length > 0 ? 1 : 0),
                // Подсчитываем слова, разделенные пробелами, переносами строк и табуляцией
                WordCount = content.Split(new[] { ' ', '\n', '\r', '\t' }, StringSplitOptions.RemoveEmptyEntries).Length,
                // Подсчитываем общее количество символов
                CharCount = content.Length
            };

            _logger.LogInformation($"Анализ файла {analysis.FileName}: {analysis.LineCount} строк, {analysis.WordCount} слов, {analysis.CharCount} символов");
            return analysis;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при анализе файла {filePath}");
            throw;
        }
    }

    /// <summary>
    /// Сохраняет результаты анализа в текстовый файл
    /// </summary>
    /// <param name="filePath">Путь к файлу для сохранения результатов</param>
    /// <param name="analysis">Результаты анализа</param>
    public async Task SaveAnalysisResultAsync(string filePath, FileAnalysis analysis)
    {
        try
        {
            // Формируем текст с результатами анализа
            var result = new StringBuilder();
            result.AppendLine($"Анализ файла: {analysis.FileName}");
            result.AppendLine($"Количество строк: {analysis.LineCount}");
            result.AppendLine($"Количество слов: {analysis.WordCount}");
            result.AppendLine($"Количество символов: {analysis.CharCount}");
            result.AppendLine(new string('-', 40)); // Разделитель между результатами

            // Добавляем результаты в конец файла
            await File.AppendAllTextAsync(filePath, result.ToString());
            _logger.LogInformation($"Результаты анализа сохранены в {filePath}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Ошибка при сохранении результатов анализа в {filePath}");
            throw;
        }
    }
} 