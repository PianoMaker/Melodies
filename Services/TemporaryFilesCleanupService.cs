using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Hosting;

namespace Melodies25.Services
{
    public class TemporaryFilesCleanupOptions
    {
        public string RelativePath { get; set; } = "melodies/temporary";
        public int MaxFileAgeMinutes { get; set; } = 12 * 60;
        public int IntervalMinutes { get; set; } = 30;
        public string SearchPattern { get; set; } = "*.*";
        public bool Enabled { get; set; } = true;
        public bool DryRun { get; set; } = false;
    }

    public class TemporaryFilesCleanupService : BackgroundService
    {
        private readonly ILogger<TemporaryFilesCleanupService> _logger;
        private readonly IWebHostEnvironment _env;
        private readonly IOptionsMonitor<TemporaryFilesCleanupOptions> _opts;

        public TemporaryFilesCleanupService(
            ILogger<TemporaryFilesCleanupService> logger,
            IWebHostEnvironment env,
            IOptionsMonitor<TemporaryFilesCleanupOptions> opts)
        {
            _logger = logger;
            _env = env;
            _opts = opts;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("[TempCleanup] Service started.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var options = _opts.CurrentValue;
                    if (options.Enabled)
                    {
                        await RunCleanupOnce(options, stoppingToken);
                    }
                    else
                    {
                        _logger.LogDebug("[TempCleanup] Disabled by configuration.");
                    }

                    var delay = TimeSpan.FromMinutes(Math.Max(1, options.IntervalMinutes));
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) { break; }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TempCleanup] Unexpected error in background cleanup loop.");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("[TempCleanup] Service stopped.");
        }

        private Task RunCleanupOnce(TemporaryFilesCleanupOptions options, CancellationToken ct)
        {
            return Task.Run(() =>
            {
                try
                {
                    var root = _env.WebRootPath ?? AppContext.BaseDirectory;
                    var target = Path.Combine(root, options.RelativePath ?? string.Empty);
                    var fullTarget = Path.GetFullPath(target);
                    var fullRoot = Path.GetFullPath(root);

                    if (!fullTarget.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogWarning("[TempCleanup] Target folder '{Target}' is outside web root. Skipping.", target);
                        return;
                    }

                    if (!Directory.Exists(fullTarget))
                    {
                        _logger.LogDebug("[TempCleanup] Folder does not exist: {Folder}", fullTarget);
                        return;
                    }

                    var maxAge = TimeSpan.FromMinutes(Math.Max(0, options.MaxFileAgeMinutes));
                    var threshold = DateTime.UtcNow - maxAge;

                    var files = Directory.EnumerateFiles(fullTarget, options.SearchPattern ?? "*.*", SearchOption.TopDirectoryOnly);

                    int removed = 0, skippedLocked = 0, errors = 0, checkedFiles = 0;

                    foreach (var file in files)
                    {
                        if (ct.IsCancellationRequested) break;
                        checkedFiles++;
                        try
                        {
                            DateTime lastWrite = File.GetLastWriteTimeUtc(file);
                            if (lastWrite > threshold)
                                continue;

                            try
                            {
                                using (var stream = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                                {
                                    // file not in use
                                }
                            }
                            catch (IOException)
                            {
                                skippedLocked++;
                                _logger.LogDebug("[TempCleanup] File locked, skipping: {File}", file);
                                continue;
                            }
                            catch (UnauthorizedAccessException)
                            {
                                skippedLocked++;
                                _logger.LogDebug("[TempCleanup] No access to file, skipping: {File}", file);
                                continue;
                            }

                            if (options.DryRun)
                            {
                                _logger.LogInformation("[TempCleanup][DryRun] Would delete: {File}", file);
                            }
                            else
                            {
                                try
                                {
                                    File.Delete(file);
                                    removed++;
                                    _logger.LogInformation("[TempCleanup] Deleted: {File}", file);
                                }
                                catch (Exception exDel)
                                {
                                    errors++;
                                    _logger.LogWarning(exDel, "[TempCleanup] Failed to delete: {File}", file);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            errors++;
                            _logger.LogWarning(ex, "[TempCleanup] Error processing file {File}", file);
                        }
                    }

                    // optionally remove empty subfolders
                    try
                    {
                        var subdirs = Directory.EnumerateDirectories(fullTarget);
                        foreach (var d in subdirs)
                        {
                            if (ct.IsCancellationRequested) break;
                            try
                            {
                                if (!Directory.EnumerateFileSystemEntries(d).Any())
                                {
                                    if (!options.DryRun)
                                    {
                                        Directory.Delete(d);
                                        _logger.LogInformation("[TempCleanup] Deleted empty folder: {Dir}", d);
                                    }
                                    else
                                    {
                                        _logger.LogInformation("[TempCleanup][DryRun] Would delete empty folder: {Dir}", d);
                                    }
                                }
                            }
                            catch (Exception exd)
                            {
                                _logger.LogDebug(exd, "[TempCleanup] Could not delete subdir {Dir}", d);
                            }
                        }
                    }
                    catch { /* ignore */ }

                    _logger.LogInformation("[TempCleanup] Completed. Checked: {Checked}, Removed: {Removed}, LockedSkipped: {Locked}, Errors: {Errors}", checkedFiles, removed, skippedLocked, errors);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "[TempCleanup] Cleanup iteration failed.");
                }
            }, ct);
        }
    }
}