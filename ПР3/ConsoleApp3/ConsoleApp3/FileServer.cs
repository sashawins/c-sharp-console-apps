using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ConsoleApp3;

/// <summary>
/// Класс, реализующий TCP-сервер для приема и обработки текстовых файлов
/// </summary>
public class FileServer
{
    private readonly TcpListener _listener;
    private readonly string _saveDirectory;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private readonly ILogger<FileServer> _logger;
    private readonly FileAnalyzer _fileAnalyzer;
    private readonly string _analysisFilePath;
    private const int MaxFileNameLength = 260; // Максимальная длина имени файла в Windows
    private const long MaxFileSize = 100 * 1024 * 1024; // 100 MB

    /// <summary>
    /// Создает новый экземпляр сервера
    /// </summary>
    /// <param name="port">Порт для прослушивания</param>
    /// <param name="saveDirectory">Директория для сохранения файлов</param>
    /// <param name="logger">Логгер для сервера</param>
    /// <param name="analyzerLogger">Логгер для анализатора файлов</param>
    public FileServer(int port, string saveDirectory, ILogger<FileServer> logger, ILogger<FileAnalyzer> analyzerLogger)
    {
        _listener = new TcpListener(IPAddress.Any, port);
        _saveDirectory = saveDirectory;
        _cancellationTokenSource = new CancellationTokenSource();
        _logger = logger;
        _fileAnalyzer = new FileAnalyzer(analyzerLogger);
        _analysisFilePath = Path.Combine(_saveDirectory, "analysis_result.txt");

        // Создаем директорию для сохранения файлов, если она не существует
        Directory.CreateDirectory(_saveDirectory);
    }

    /// <summary>
    /// Запускает сервер и начинает прослушивание подключений
    /// </summary>
    public async Task StartAsync()
    {
        try
        {
            _listener.Start();
            _logger.LogInformation("Сервер запущен и ожидает подключений...");

            // Основной цикл обработки подключений
            while (!_cancellationTokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    _ = HandleClientAsync(client); // Асинхронная обработка клиента
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Ошибка при подключении клиента");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при работе сервера");
            throw;
        }
        finally
        {
            _listener.Stop();
        }
    }

    /// <summary>
    /// Обрабатывает подключение клиента
    /// </summary>
    /// <param name="client">TCP-клиент</param>
    private async Task HandleClientAsync(TcpClient client)
    {
        try
        {
            using var stream = client.GetStream();
            using var reader = new BinaryReader(stream);
            using var writer = new BinaryWriter(stream);

            try
            {
                // Проверяем имя файла
                var fileNameLength = reader.ReadInt32();
                if (fileNameLength <= 0 || fileNameLength > MaxFileNameLength)
                {
                    throw new InvalidOperationException($"Некорректная длина имени файла: {fileNameLength}");
                }

                var fileNameBytes = reader.ReadBytes(fileNameLength);
                var originalFileName = Encoding.UTF8.GetString(fileNameBytes);

                // Проверяем имя файла
                if (string.IsNullOrWhiteSpace(originalFileName) || originalFileName.Contains(".."))
                {
                    throw new InvalidOperationException("Некорректное имя файла");
                }

                // Проверяем размер файла
                var fileSize = reader.ReadInt64();
                if (fileSize <= 0 || fileSize > MaxFileSize)
                {
                    throw new InvalidOperationException($"Некорректный размер файла: {fileSize} байт");
                }

                // Генерируем уникальное имя файла и сохраняем его
                var uniqueFileName = $"{Guid.NewGuid()}_{originalFileName}";
                var filePath = Path.Combine(_saveDirectory, uniqueFileName);

                // Читаем и сохраняем содержимое файла
                using (var fileStream = File.Create(filePath))
                {
                    var buffer = new byte[8192];
                    var bytesRead = 0;
                    var totalBytesRead = 0L;

                    while (totalBytesRead < fileSize && (bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await fileStream.WriteAsync(buffer, 0, bytesRead);
                        totalBytesRead += bytesRead;
                    }

                    // Проверяем, что файл был передан полностью
                    if (totalBytesRead != fileSize)
                    {
                        throw new InvalidOperationException("Неполная передача файла");
                    }
                }

                _logger.LogInformation($"Файл {originalFileName} успешно сохранен как {uniqueFileName}");

                // Анализируем файл и сохраняем результаты
                var analysis = await _fileAnalyzer.AnalyzeFileAsync(filePath);
                await _fileAnalyzer.SaveAnalysisResultAsync(_analysisFilePath, analysis);

                // Отправляем результаты анализа клиенту
                var resultMessage = $"Имя файла: {originalFileName}\n" +
                                  $"Строк: {analysis.LineCount}, " +
                                  $"Слов: {analysis.WordCount}, " +
                                  $"Символов: {analysis.CharCount}";
                
                var resultBytes = Encoding.UTF8.GetBytes(resultMessage);
                writer.Write(resultBytes.Length);
                writer.Write(resultBytes);
                await writer.BaseStream.FlushAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при обработке запроса клиента");
                
                // Отправляем сообщение об ошибке клиенту
                var errorMessage = $"Ошибка: {ex.Message}";
                var errorBytes = Encoding.UTF8.GetBytes(errorMessage);
                writer.Write(errorBytes.Length);
                writer.Write(errorBytes);
                await writer.BaseStream.FlushAsync();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при работе с клиентом");
        }
        finally
        {
            client.Close();
        }
    }

    /// <summary>
    /// Останавливает сервер
    /// </summary>
    public void Stop()
    {
        _cancellationTokenSource.Cancel();
    }
} 