using System.IO;
using System.Text.Json;
using Melodies25.Models;
using Microsoft.AspNetCore.Hosting;

namespace Melodies25.Utilities
{
    // Lightweight replacement for previous FileSettingsProvider.
    // Provides runtime access and persistence for LoggingSettings.
    public static class LoggingManager
    {
        private static readonly object _sync = new();
        private static string _filePath = string.Empty;

        public static LoggingSettings Settings { get; private set; } = new LoggingSettings();

        public static bool AlgorithmDiagnostics => Settings?.AlgorithmDiagnostics ?? true;
        public static bool Comparing => Settings?.Comparing ?? true;
        public static bool PatternSearch => Settings?.PatternSearch ?? false;
        public static bool DbQueries => Settings?.DbQueries ?? false;
        public static bool CreateAudio => Settings?.CreateAudio ?? false;
        public static bool ReadMidi => Settings?.CompareMidi ?? false;

        // Call once at startup (optional) to load persisted file if exists
        public static void Initialize(IWebHostEnvironment env, string fileName = "loggingsettings.json")
        {
            if (env is null) return;
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
                        if (loaded != null) Settings = loaded;
                    }
                    catch
                    {
                        // ignore -> use defaults
                    }
                }
                else
                {
                    // ensure file created with defaults
                    Save(Settings);
                }
            }
        }

        public static void Save(LoggingSettings settings)
        {
            lock (_sync)
            {
                Settings = settings ?? new LoggingSettings();
                if (string.IsNullOrEmpty(_filePath))
                    return; // not initialized; skip persistence

                try
                {
                    var opts = new JsonSerializerOptions { WriteIndented = true };
                    var json = JsonSerializer.Serialize(Settings, opts);
                    File.WriteAllText(_filePath, json);
                }
                catch
                {
                    // ignore persistence errors
                }
            }
        }
    }
}
