using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//trdytfuygiuhiojp
namespace ConsoleApp1
{
    public class Logger
    {
        private readonly string _logFilePath;
        private readonly object _lockObject = new object();

        public Logger(string logFilePath)
        {
            _logFilePath = logFilePath;
        }

        protected void WriteLog(string level, string message)
        {
            lock (_lockObject)
            {
                var logEntry =
                    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
            }
        }

        public void LogInfo(string message) => WriteLog("INFO", message);
        public void LogWarning(string message) => WriteLog("WARNING", message);

        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null
                ? $"{message}: {ex.Message}{Environment.NewLine}{ex.StackTrace}"
                : message;
            WriteLog("ERROR", fullMessage);
        }

        public void LogDebug(string message) => WriteLog("DEBUG", message);
    }

}
