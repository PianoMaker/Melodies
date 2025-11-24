using System.IO;
using System.Text.Json;
using Melodies25.Models;
using Microsoft.AspNetCore.Hosting;

namespace Melodies25.Utilities
{
    // Simple file-backed settings provider (thread-safe-ish for basic use)
    public static class FileSettingsProvider
    {
        private static readonly object _sync = new();
        private static string _filePath = string.Empty;
        public static LoggingSettings Logging { get; private set; } = new LoggingSettings();

        // Call once at startup to set file location and load file if exists
        public static void Initialize(IWebHostEnvironment env, string fileName = "loggingsettings.json")
        {
            lock (_sync)
            {
                _filePath = Path.Combine(env.ContentRootPath, fileName);
                if (File.Exists(_filePath))
                {
                    try
                    {
                        var json = File.ReadAllText(_filePath);
                        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                        var loaded = JsonSerializer.Deserialize<LoggingSettings>(json, opts);
                        if (loaded != null) Logging = loaded;
                    }
                    catch
                    {
                        // ignore -> use defaults
                    }
                }
                else
                {
                    // ensure parent exists and write default
                    Save(Logging);
                }
            }
        }

        public static void Save(LoggingSettings settings)
        {
            lock (_sync)
            {
                Logging = settings ?? new LoggingSettings();
                var opts = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(Logging, opts);
                File.WriteAllText(_filePath, json);
            }
        }
    }
}