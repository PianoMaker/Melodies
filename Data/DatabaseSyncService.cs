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

        // NEW: Enrich English names in both databases (NameEn, SurnameEn) after author existence sync
        await SyncAuthorEnglishNamesAsync();

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

    // NEW: two-way enrichment of NameEn and SurnameEn
    private async Task SyncAuthorEnglishNamesAsync()
    {
        _logger.LogInformation("Enriching English author names between SOURCE and TARGET ...");

        // Load tracked entities for updates
        var srcAuthors = await _sourceDb.Author.ToListAsync();
        var trgAuthors = await _targetDb.Author.ToListAsync();

        // Index by normalized (surname, name)
        string Key(Author a)
        {
            var s = ((a.Surname ?? "").Trim().ToLower());
            var n = ((a.Name ?? "").Trim().ToLower());
            return $"{s}|{n}";
        }

        // Build grouped map to avoid duplicate-key exception when multiple source rows normalize to same key.
        var grouped = srcAuthors
            .Where(a => !string.IsNullOrWhiteSpace(a.Surname) || !string.IsNullOrWhiteSpace(a.Name))
            .GroupBy(a => Key(a));

        // Log duplicates and create dictionary choosing first entry per key
        var duplicates = grouped.Where(g => g.Count() > 1).ToList();
        foreach (var g in duplicates)
        {
            AddCollision($"Duplicate normalized author key in SOURCE: '{g.Key}' ({g.Count()} entries). Using the first occurrence.");
            _logger.LogDebug("Duplicate group members: {Members}", string.Join(" | ", g.Select(x => $"{x.Surname} {x.Name} (Id={x.ID})")));
        }

        var srcMap = grouped.ToDictionary(g => g.Key, g => g.First());

        int updatedSrc = 0, updatedTrg = 0, conflicts = 0;

        foreach (var t in trgAuthors)
        {
            var key = Key(t);
            if (!srcMap.TryGetValue(key, out var s))
                continue; // author not in source — skip

            // Compare and fill when one side is empty and the other has data
            var sNameEn = (s.NameEn ?? "").Trim();
            var sSurnameEn = (s.SurnameEn ?? "").Trim();
            var tNameEn = (t.NameEn ?? "").Trim();
            var tSurnameEn = (t.SurnameEn ?? "").Trim();

            bool trgChanged = false, srcChanged = false;

            // NameEn
            if (string.IsNullOrWhiteSpace(tNameEn) && !string.IsNullOrWhiteSpace(sNameEn))
            {
                t.NameEn = sNameEn;
                trgChanged = true; updatedTrg++;
            }
            else if (string.IsNullOrWhiteSpace(sNameEn) && !string.IsNullOrWhiteSpace(tNameEn))
            {
                s.NameEn = tNameEn;
                srcChanged = true; updatedSrc++;
            }
            else if (!string.IsNullOrWhiteSpace(tNameEn) && !string.IsNullOrWhiteSpace(sNameEn) && !string.Equals(tNameEn, sNameEn, StringComparison.Ordinal))
            {
                conflicts++;
                AddCollision($"NameEn differs for '{t.Surname} {t.Name}': source='{sNameEn}', target='{tNameEn}'");
            }

            // SurnameEn
            if (string.IsNullOrWhiteSpace(tSurnameEn) && !string.IsNullOrWhiteSpace(sSurnameEn))
            {
                t.SurnameEn = sSurnameEn;
                if (!trgChanged) updatedTrg++;
                trgChanged = true;
            }
            else if (string.IsNullOrWhiteSpace(sSurnameEn) && !string.IsNullOrWhiteSpace(tSurnameEn))
            {
                s.SurnameEn = tSurnameEn;
                if (!srcChanged) updatedSrc++;
                srcChanged = true;
            }
            else if (!string.IsNullOrWhiteSpace(tSurnameEn) && !string.IsNullOrWhiteSpace(sSurnameEn) && !string.Equals(tSurnameEn, sSurnameEn, StringComparison.Ordinal))
            {
                conflicts++;
                AddCollision($"SurnameEn differs for '{t.Surname} {t.Name}': source='{sSurnameEn}', target='{tSurnameEn}'");
            }
        }

        if (updatedTrg > 0)
        {
            await _targetDb.SaveChangesAsync();
            _logger.LogInformation("Updated target English fields for {Count} authors.", updatedTrg);
        }
        else
        {
            _logger.LogInformation("No English fields updated in target.");
        }

        if (updatedSrc > 0)
        {
            await _sourceDb.SaveChangesAsync();
            _logger.LogInformation("Updated source English fields for {Count} authors.", updatedSrc);
        }
        else
        {
            _logger.LogInformation("No English fields updated in source.");
        }

        if (conflicts > 0)
        {
            _logger.LogWarning("English name conflicts detected for {Count} authors. See collisions list.", conflicts);
        }
        _logger.LogInformation("English author names enrichment complete.");
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