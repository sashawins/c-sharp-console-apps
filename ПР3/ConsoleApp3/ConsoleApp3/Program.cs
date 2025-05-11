using Microsoft.Extensions.Logging;
using ConsoleApp3;

// Создаем фабрику логгеров с поддержкой вывода в консоль
var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddConsole();
});

// Создаем логгеры для сервера и анализатора файлов
var serverLogger = loggerFactory.CreateLogger<FileServer>();
var analyzerLogger = loggerFactory.CreateLogger<FileAnalyzer>();

// Создаем директорию для сохранения файлов в папке с приложением
var saveDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ReceivedFiles");

// Создаем и запускаем сервер на порту 5000
var server = new FileServer(5000, saveDirectory, serverLogger, analyzerLogger);

Console.WriteLine("Сервер запускается...");
Console.WriteLine("Нажмите Ctrl+C для остановки сервера");

try
{
    // Запускаем сервер и ждем подключений
    await server.StartAsync();
}
catch (Exception ex)
{
    // Логируем критические ошибки сервера
    serverLogger.LogError(ex, "Произошла ошибка при работе сервера");
}
finally
{
    // Корректно останавливаем сервер при выходе
    server.Stop();
}
