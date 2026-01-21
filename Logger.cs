using System;
using System.IO;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    public enum LogSeverity
    {
        Debug,
        Info,
        Warning,
        Error
    }

    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath
                           ?? throw new ArgumentNullException(nameof(logFilePath));
        }

        // ДЕКОМПОЗИЦИЯ УСЛОВНОГО ОПЕРАТОРА:
        // отдельный метод для формирования текстового сообщения об ошибке.
        private static string BuildErrorMessage(string message, Exception? ex)
        {
            if (ex == null)
                return message;

            return $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}";
        }

        // КОНСОЛИДАЦИЯ ДУБЛИРУЮЩИХСЯ ФРАГМЕНТОВ:
        // один универсальный метод логирования по enum‑уровню, вместо
        // четырёх почти одинаковых методов, которые строили строки.
        protected void WriteLog(LogSeverity severity, string message)
        {
            var level = MapSeverityToString(severity);
            var logEntry = FormatLogEntry(level, message);

            lock (_lockObject)
            {
                File.AppendAllText(_logFilePath, logEntry);
            }
        }

        // КОНСОЛИДАЦИЯ УСЛОВНОГО ВЫРАЖЕНИЯ:
        // все проверки уровня сведены в switch‑выражение,
        // вместо набора if/else по строкам.
        private static string MapSeverityToString(LogSeverity severity) =>
            severity switch
            {
                LogSeverity.Debug => "DEBUG",
                LogSeverity.Info => "INFO",
                LogSeverity.Warning => "WARNING",
                LogSeverity.Error => "ERROR",
                _ => "INFO"
            };

        private static string FormatLogEntry(string level, string message)
        {
            return $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}{Environment.NewLine}";
        }

        // «Фасадные» методы вызывают общий WriteLog.
        public void LogInfo(string message) =>
            WriteLog(LogSeverity.Info, message);

        public void LogWarning(string message) =>
            WriteLog(LogSeverity.Warning, message);

        public void LogDebug(string message) =>
            WriteLog(LogSeverity.Debug, message);

        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = BuildErrorMessage(message, ex);
            WriteLog(LogSeverity.Error, fullMessage);
        }

        // Пример асинхронного метода без управляющего флага:
        // раньше часто делают bool success = false и в конце if (success) ...
        // здесь такого флага нет, просто пробуем и логируем при ошибке.
        public async Task TryLogAsync(LogSeverity severity, string message)
        {
            try
            {
                await Task.Run(() => WriteLog(severity, message));
            }
            catch (IOException ioEx)
            {
                // здесь можно отправить в резервный лог или вообще проглотить
                Console.Error.WriteLine($"Logging failed: {ioEx.Message}");
            }
        }
    }
}
