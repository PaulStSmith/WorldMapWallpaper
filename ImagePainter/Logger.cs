using System.Runtime.CompilerServices;

namespace WorldMapWallpaper
{
    public class Logger
    {
        public string LogFile 
        {
            get
            {
#pragma warning disable CS8604 // Location is non-null
                _LogFile ??= Path.Combine(AppContext.BaseDirectory, "log", "WorldMapWallpaper.log");
#pragma warning restore CS8604 // Possible null reference argument.
                return _LogFile;
            }
        }
        string? _LogFile;

        /// <summary>
        /// Logs a debug message.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="caller">The name of the calling method. This is automatically set by the compiler.</param>
        public void Debug(string msg, [CallerMemberName] string caller = "")
        {
            WriteLog("DEBUG", msg, caller);
        }

        /// <summary>
        /// Logs an informational message.
        /// </summary>
        /// <param name="msg">The message to log.</param>
        /// <param name="caller">The name of the calling method. This is automatically set by the compiler.</param>
        public void Info(string msg, [CallerMemberName] string caller = "")
        {
            WriteLog("INFO", msg, caller);
        }

        private void WriteLog(string level, string msg, string caller)
        {
            var log = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level,-6}] [{caller}] {msg}";

            try
            {
                var dir = Path.GetDirectoryName(LogFile);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!);

                using var writer = new StreamWriter(LogFile, true);
                writer.WriteLine(log);
            }
            catch (Exception)
            {
                // If we can't write to the log file, just continue silently
                // This prevents the application from crashing due to logging issues
            }
        }
    }
}