namespace Melodies25.Models
{
    public class LoggingSettings
    {
        // Controls printing algorithm diagnostics (match lists)
        public bool AlgorithmDiagnostics { get; set; } = true;

        // Controls printing timing information (Stopwatch outputs)
        public bool Comparing { get; set; } = true;

        // Other toggles you may want
        public bool PatternSearch { get; set; } = true;
        public bool DbQueries { get; set; } = false;

        // NEW: granular toggles for new logging targets
        // Log whenever audio (mp3) is created from a pattern/midi
        public bool CreateAudio { get; set; } = false;

        // Log detailed compare/diagnostics for midi comparison routines
        public bool CompareMidi { get; set; } = false;
    }
}