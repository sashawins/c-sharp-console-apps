using System.Net.Sockets;
using System.Text;
using Microsoft.Extensions.Logging;

namespace ConsoleApp3;

public class FileClient
{
    private readonly string _serverAddress;
    private readonly int _serverPort;
    private readonly ILogger<FileClient> _logger;
    private const int MaxRetries = 3;
    private const int RetryDelayMs = 1000;

    public FileClient(string serverAddress, int serverPort, ILogger<FileClient> logger)
    {
        _serverAddress = serverAddress;
        _serverPort = serverPort;
        _logger = logger;
    }

    public async Task SendFileAsync(string filePath)
    {
        var retryCount = 0;
        while (true)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(_serverAddress, _serverPort);
                _logger.LogInformation($"Подключено к серверу {_serverAddress}:{_serverPort}");

                using var stream = client.GetStream();
                using var writer = new BinaryWriter(stream);
                using var reader = new BinaryReader(stream);

                // Проверяем существование файла
                if (!File.Exists(filePath))
                {
                    throw new FileNotFoundException($"Файл не найден: {filePath}");
                }

                // Проверяем размер файла
                var fileInfo = new FileInfo(filePath);
                if (fileInfo.Length == 0)
                {
                    throw new InvalidOperationException("Файл пуст");
                }

                // Отправляем имя файла
                var fileName = Path.GetFileName(filePath);
                var fileNameBytes = Encoding.UTF8.GetBytes(fileName);
                writer.Write(fileNameBytes.Length);
                writer.Write(fileNameBytes);

                // Отправляем размер файла
                writer.Write(fileInfo.Length);

                // Отправляем содержимое файла
                using (var fileStream = File.OpenRead(filePath))
                {
                    var buffer = new byte[8192];
                    int bytesRead;
                    while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                    {
                        await stream.WriteAsync(buffer, 0, bytesRead);
                    }
                }

                _logger.LogInformation($"Файл {fileName} отправлен на сервер");

                // Получаем результаты анализа или сообщение об ошибке
                var resultLength = reader.ReadInt32();
                var resultBytes = reader.ReadBytes(resultLength);
                var result = Encoding.UTF8.GetString(resultBytes);

                if (result.StartsWith("Ошибка:"))
                {
                    throw new Exception(result);
                }

                Console.WriteLine("\nРезультаты анализа:");
                Console.WriteLine(result);
                return; // Успешное завершение
            }
            catch (SocketException ex)
            {
                retryCount++;
                if (retryCount >= MaxRetries)
                {
                    _logger.LogError(ex, "Не удалось подключиться к серверу после нескольких попыток");
                    throw new Exception("Не удалось подключиться к серверу. Проверьте, что сервер запущен и доступен.");
                }
                _logger.LogWarning($"Ошибка подключения, повторная попытка {retryCount} из {MaxRetries}...");
                await Task.Delay(RetryDelayMs);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при отправке файла");
                throw;
            }
        }
    }
} 