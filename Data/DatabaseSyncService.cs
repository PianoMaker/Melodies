using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseSyncService
{
    private readonly Melodies25SourceContext _sourceDb;   // VisualStudio (source)
    private readonly Melodies25TargetContext _targetDb;   // SQLExpress (target)
    private readonly ILogger<DatabaseSyncService> _logger;

    private List<Author> _missingAuthorsInTarget = new();
    private List<Country> _missingCountriesInTarget = new();
    private List<Melody> _missingMelodiesInTarget = new();

    private readonly List<string> _collisions = new();
    public IReadOnlyList<string> Collisions => _collisions;

    public DatabaseSyncService(Melodies25SourceContext sourceDb, Melodies25TargetContext targetDb, ILogger<DatabaseSyncService> logger)
    {
        _sourceDb = sourceDb;
        _targetDb = targetDb;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> SyncDatabasesAsync()
    {
        _collisions.Clear();
        _logger.LogInformation("Starting one-way sync SOURCE -> TARGET ...");

        bool countries = await AnalyzeCountriesAsync();
        bool authors = await AnalyzeAuthorsAsync();
        bool melodies = await AnalyzeMelodiesAsync();

        if (countries) await SyncCountriesAsync(); else _logger.LogInformation("Countries up-to-date.");
        if (authors) await SyncAuthorsAsync(); else _logger.LogInformation("Authors up-to-date.");
        if (melodies) await SyncMelodiesAsync(); else _logger.LogInformation("Melodies up-to-date.");

        _logger.LogInformation("Synchronization finished.");
        return Collisions;
    }

    private void AddCollision(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        _collisions.Add(msg);
        _logger.LogWarning("COLLISION: {Msg}", msg);
    }

    private async Task<bool> AnalyzeCountriesAsync()
    {
        var src = await _sourceDb.Country.AsNoTracking().ToListAsync();
        var trg = await _targetDb.Country.AsNoTracking().ToListAsync();
        _missingCountriesInTarget = src.Where(sc => !trg.Any(tc => Same(tc.Name, sc.Name))).ToList();
        _logger.LogInformation("Missing countries in target: {Count}", _missingCountriesInTarget.Count);
        return _missingCountriesInTarget.Any();
    }

    private async Task<bool> AnalyzeAuthorsAsync()
    {
        var src = await _sourceDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        var trg = await _targetDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        _missingAuthorsInTarget = src.Where(sa => !trg.Any(ta => Same(ta.Name, sa.Name) && Same(ta.Surname, sa.Surname))).ToList();
        _logger.LogInformation("Missing authors in target: {Count}", _missingAuthorsInTarget.Count);
        return _missingAuthorsInTarget.Any();
    }

    private async Task<bool> AnalyzeMelodiesAsync()
    {
        var src = await _sourceDb.Melody.AsNoTracking().Include(m => m.Author).ThenInclude(a => a.Country).ToListAsync();
        var trg = await _targetDb.Melody.AsNoTracking().Include(m => m.Author).ThenInclude(a => a.Country).ToListAsync();
        _missingMelodiesInTarget = src.Where(sm => !trg.Any(tm => Same(tm.Title, sm.Title) && Same(tm.Author?.Surname, sm.Author?.Surname))).ToList();
        _logger.LogInformation("Missing melodies in target: {Count}", _missingMelodiesInTarget.Count);
        return _missingMelodiesInTarget.Any();
    }

    private async Task SyncCountriesAsync()
    {
        foreach (var c in _missingCountriesInTarget) c.ID = 0;
        if (_missingCountriesInTarget.Any())
        {
            var existing = new HashSet<string>(await _targetDb.Country.Select(x => x.Name).ToListAsync(), StringComparer.OrdinalIgnoreCase);
            var toAdd = _missingCountriesInTarget.Where(c => !existing.Contains(c.Name)).ToList();
            if (toAdd.Any())
            {
                _targetDb.Country.AddRange(toAdd);
                await _targetDb.SaveChangesAsync();
                _logger.LogInformation("Added countries: {List}", string.Join(", ", toAdd.Select(c => c.Name)));
            }
        }
        _logger.LogInformation("Countries sync complete.");
    }

    private async Task SyncAuthorsAsync()
    {
        foreach (var a in _missingAuthorsInTarget) a.ID = 0;
        foreach (var author in _missingAuthorsInTarget)
        {
            Country? country = null;
            if (author.Country != null)
            {
                var norm = author.Country.Name?.Trim().ToLower();
                if (!string.IsNullOrEmpty(norm))
                {
                    country = await _targetDb.Country.FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == norm);
                    if (country == null)
                    {
                        country = new Country { Name = author.Country.Name };
                        _targetDb.Country.Add(country);
                        await _targetDb.SaveChangesAsync();
                    }
                }
            }

            // Normalize values to avoid using custom method in query (EF cannot translate custom method Same)
            var normSurname = (author.Surname ?? "").Trim().ToLower();
            var normName = (author.Name ?? "").Trim().ToLower();
            bool exists = await _targetDb.Author.AnyAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname && ((a.Name ?? "").Trim().ToLower()) == normName);
            if (exists)
            {
                AddCollision($"Author '{author.Surname} {author.Name}' already exists (skip).");
                continue;
            }

            _targetDb.Author.Add(new Author
            {
                Name = author.Name,
                Surname = author.Surname,
                Country = country,
                DateOfBirth = author.DateOfBirth,
                DateOfDeath = author.DateOfDeath,
                Description = author.Description
            });
        }
        await _targetDb.SaveChangesAsync();
        _logger.LogInformation("Authors sync complete.");
    }

    private async Task SyncMelodiesAsync()
    {
        foreach (var melody in _missingMelodiesInTarget)
        {
            if (melody.Author == null) continue;
            var normSurname = (melody.Author.Surname ?? "").Trim().ToLower();
            var author = await _targetDb.Author.Include(a => a.Country)
                .FirstOrDefaultAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname);

            if (author == null)
            {
                // Ensure author (and country) exists in target
                Country? country = null;
                if (melody.Author.Country != null)
                {
                    var normCountry = (melody.Author.Country.Name ?? "").Trim().ToLower();
                    country = await _targetDb.Country.FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountry);
                    if (country == null)
                    {
                        country = new Country { Name = melody.Author.Country.Name };
                        _targetDb.Country.Add(country);
                        await _targetDb.SaveChangesAsync();
                    }
                }
                author = new Author
                {
                    Name = melody.Author.Name,
                    Surname = melody.Author.Surname,
                    Country = country,
                    DateOfBirth = melody.Author.DateOfBirth,
                    DateOfDeath = melody.Author.DateOfDeath,
                    Description = melody.Author.Description
                };
                _targetDb.Author.Add(author);
                await _targetDb.SaveChangesAsync();
            }

            var normTitle = (melody.Title ?? "").Trim().ToLower();
            bool exists = await _targetDb.Melody.AnyAsync(m => ((m.Title ?? "").Trim().ToLower()) == normTitle && m.AuthorID == author.ID);
            if (exists)
            {
                AddCollision($"Melody '{melody.Title}' ({melody.Author.Surname}) already exists (skip).");
                continue;
            }

            _targetDb.Melody.Add(new Melody
            {
                Title = melody.Title,
                Author = author,
                Year = melody.Year,
                Description = melody.Description,
                FilePath = melody.FilePath,
                IsFileEligible = melody.IsFileEligible,
                Tonality = melody.Tonality,
                AuthorID = author.ID
            });
        }
        await _targetDb.SaveChangesAsync();
        _logger.LogInformation("Melodies sync complete.");
    }

    private static bool Same(string a, string b) => string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}