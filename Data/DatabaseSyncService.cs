using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;
using System.Text;

public class DatabaseSyncService
{
    private readonly Melodies25Context _sourceDb;
    private readonly Melodies25TargetContext _targetDb;
    private readonly ILogger<DatabaseSyncService> _logger;

    private List<Author> missingAuthorsInLocal;
    private List<Country> countriesPresentOnlyInSource;
    private List<Melody> missingMelodiesInSource;

    private readonly List<string> _collisions = new();
    public IReadOnlyList<string> Collisions => _collisions;

    public DatabaseSyncService(Melodies25Context sourceDb, Melodies25TargetContext sqlTargetDb, ILogger<DatabaseSyncService> logger)
    {
        _sourceDb = sourceDb;
        _targetDb = sqlTargetDb;
        _logger = logger;
    }

    // Повертаємо список колізій щоб UI міг показати toast
    public async Task<IReadOnlyList<string>> SyncDatabasesAsync()
    {
        _collisions.Clear();
        _logger.LogInformation("Початок синхронізації баз даних...");

        bool ifMissingCountries = await AnalyzeCountries();
        bool ifMissingAuthors = await AnalyzeAuthors();
        bool ifMissingMelodies = await AnalyzeMelodies();

        _logger.LogInformation("Перевірка на відсутні записи завершена.");

        if (ifMissingCountries)
            await SyncCountries();
        else
            _logger.LogInformation("skip countries upd.");

        if (ifMissingAuthors)
            await SyncAuthors();
        else
            _logger.LogInformation("skip authors upd.");

        if (ifMissingMelodies)
            await SyncMelodies();
        else
            _logger.LogInformation("skip melodies upd.");

        _logger.LogInformation("Синхронізація баз даних завершена.");
        return Collisions;
    }

    private void AddCollision(string msg)
    {
        if (string.IsNullOrWhiteSpace(msg)) return;
        _collisions.Add(msg);
        _logger.LogWarning("COLLISION: {Msg}", msg);
    }

    private async Task<bool> AnalyzeMelodies()
    {
        bool ifmissingmelodies = false;

        var sourceMelodies = await _sourceDb.Melody
            .AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        var targetMelodies = await _targetDb.Melody
            .AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        _logger.LogInformation("\nМелодій у локальній БД (source): {MelodyCount}", sourceMelodies.Count);
        _logger.LogInformation("Мелодій у цільовій БД (target): {MelodyCount}", targetMelodies.Count);

        missingMelodiesInSource = targetMelodies
            .Where(em => !sourceMelodies.Any(lm => Same(em.Title, lm.Title) && Same(em.Author?.Surname, lm.Author?.Surname)))
            .ToList();

        if (missingMelodiesInSource.Any())
        {
            var melodyTitles = string.Join(", ", missingMelodiesInSource.Select(m => $"\n{m.Title} ({m.Author?.Name} {m.Author?.Surname})"));
            _logger.LogInformation("Відсутні мелодії у source: {MelodyTitles}", melodyTitles);
            ifmissingmelodies = true;
        }
        else
            _logger.LogInformation("\nВідсутніх мелодій не виявлено");

        return ifmissingmelodies;
    }

    private async Task<bool> AnalyzeAuthors()
    {
        bool ifmissingauthors = false;
        var localAuthors = await _sourceDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        var targetAuthors = await _targetDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();

        _logger.LogInformation("\nАвторів у source: {Count}", localAuthors.Count);
        _logger.LogInformation("Авторів у target: {Count}", targetAuthors.Count);

        missingAuthorsInLocal = targetAuthors
            .Where(ta => !localAuthors.Any(la => Same(la.Name, ta.Name) && Same(la.Surname, ta.Surname)))
            .ToList();

        if (missingAuthorsInLocal.Any())
        {
            var authorNames = string.Join("\n", missingAuthorsInLocal.Select(a => $"{a.Name} {a.Surname}"));
            _logger.LogInformation("\nВідсутні автори у source: {AuthorNames}", authorNames);
            ifmissingauthors = true;
        }
        else
            _logger.LogInformation("\nВідсутніх авторів не виявлено");

        return ifmissingauthors;
    }

    private async Task<bool> AnalyzeCountries()
    {
        bool ifMissingCountries = false;
        var sourceCountries = await _sourceDb.Country.AsNoTracking().ToListAsync();
        var targetCountries = await _targetDb.Country.AsNoTracking().ToListAsync();
        _logger.LogInformation("\nКраїн у source: {CountryCount}", sourceCountries.Count);
        _logger.LogInformation("Країн у target: {CountryCount}", targetCountries.Count);

        countriesPresentOnlyInSource = sourceCountries
            .Where(sc => !targetCountries.Any(tc => Same(tc.Name, sc.Name)))
            .ToList();

        if (countriesPresentOnlyInSource.Any())
        {
            var countryNames = string.Join("\n", countriesPresentOnlyInSource.Select(c => c.Name));
            _logger.LogInformation("\nВідсутні країни у target: {CountryNames}", countryNames);
            ifMissingCountries = true;
        }
        else
        {
            _logger.LogInformation("\nВідсутніх країн у target не виявлено");
        }
        return ifMissingCountries;
    }

    private async Task SyncCountries()
    {
        var sourceCountries = await _sourceDb.Country.AsNoTracking().ToListAsync();
        var targetCountries = await _targetDb.Country.AsNoTracking().ToListAsync();

        var countriesOnlyInSource = countriesPresentOnlyInSource;
        var countriesOnlyInTarget = targetCountries
            .Where(tc => !sourceCountries.Any(sc => Same(sc.Name, tc.Name)))
            .ToList();

        foreach (var c in countriesOnlyInSource) c.ID = 0;
        foreach (var c in countriesOnlyInTarget) c.ID = 0;

        if (countriesOnlyInSource.Any())
        {
            var existing = new HashSet<string>(
                await _targetDb.Country.Select(x => x.Name).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            var toAdd = countriesOnlyInSource.Where(c => !existing.Contains(c.Name)).ToList();
            var collisions = countriesOnlyInSource.Where(c => existing.Contains(c.Name))
                .Select(c => $"Country '{c.Name}' вже існує у target (пропуск)").ToList();
            collisions.ForEach(AddCollision);

            if (toAdd.Any())
            {
                _targetDb.Country.AddRange(toAdd);
                await _targetDb.SaveChangesAsync();
                _logger.LogInformation("Додано країн у target: {List}", string.Join(", ", toAdd.Select(c => c.Name)));
            }
        }

        if (countriesOnlyInTarget.Any())
        {
            var existing = new HashSet<string>(
                await _sourceDb.Country.Select(x => x.Name).ToListAsync(),
                StringComparer.OrdinalIgnoreCase);

            var toAdd = countriesOnlyInTarget.Where(c => !existing.Contains(c.Name)).ToList();
            var collisions = countriesOnlyInTarget.Where(c => existing.Contains(c.Name))
                .Select(c => $"Country '{c.Name}' вже існує у source (пропуск)").ToList();
            collisions.ForEach(AddCollision);

            if (toAdd.Any())
            {
                _sourceDb.Country.AddRange(toAdd);
                await _sourceDb.SaveChangesAsync();
                _logger.LogInformation("Додано країн у source: {List}", string.Join(", ", toAdd.Select(c => c.Name)));
            }
        }

        _logger.LogInformation("Синхронізація країн завершена.");
    }

    private async Task SyncAuthors()
    {
        var missingInLocal = missingAuthorsInLocal;
        foreach (var author in missingInLocal) author.ID = 0;
        await AddMissingAuthors(missingInLocal, _sourceDb, _targetDb);
        _logger.LogInformation("Синхронізація авторів завершена.");
    }

    private async Task AddMissingAuthors(IEnumerable<Author> missingAuthors, DbContext targetDb, DbContext sourceDb)
    {
        foreach (var author in missingAuthors)
        {
            Country country = null;
            if (author.Country != null)
            {
                var normCountryName = (author.Country.Name ?? "").Trim().ToLower();
                country = await targetDb.Set<Country>()
                    .FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountryName);

                if (country == null)
                {
                    country = await sourceDb.Set<Country>()
                        .FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountryName);

                    if (country != null)
                    {
                        bool countryExists = await targetDb.Set<Country>()
                            .AnyAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountryName);
                        if (!countryExists)
                        {
                            targetDb.Set<Country>().Add(new Country { Name = country.Name });
                            await targetDb.SaveChangesAsync();
                        }
                        country = await targetDb.Set<Country>()
                            .FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountryName);
                    }
                }
            }

            var normName = (author.Name ?? "").Trim().ToLower();
            var normSurname = (author.Surname ?? "").Trim().ToLower();
            bool exists = await targetDb.Set<Author>()
                .AnyAsync(a => ((a.Name ?? "").Trim().ToLower()) == normName &&
                               ((a.Surname ?? "").Trim().ToLower()) == normSurname);
            if (exists)
            {
                AddCollision($"Author '{author.Surname} {author.Name}' вже існує у target (пропуск).");
                continue;
            }

            var authorCopy = new Author
            {
                Name = author.Name,
                Surname = author.Surname,
                Country = country,
                DateOfBirth = author.DateOfBirth,
                DateOfDeath = author.DateOfDeath,
                Description = author.Description
            };
            targetDb.Set<Author>().Add(authorCopy);
            _logger.LogInformation("Додаємо автора {Name} {Surname}", author.Name, author.Surname);
        }
        await targetDb.SaveChangesAsync();
    }

    private async Task SyncMelodies()
    {
        _logger.LogInformation("method syncMelodies starts.");
        var sourceMelodies = await _sourceDb.Melody.AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        var targetMelodies = await _targetDb.Melody.AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        var missingInSource = missingMelodiesInSource;
        var missingInTarget = sourceMelodies.Where(lm =>
            !targetMelodies.Any(em => Same(em.Title, lm.Title) && Same(em.Author?.Surname, lm.Author?.Surname)))
            .ToList();

        _logger.LogInformation("missing in source - {Count}.", missingInSource.Count);
        _logger.LogInformation("missing in target - {Count}.", missingInTarget.Count);

        await AddMissingMelodies(missingInSource, _sourceDb, _targetDb);
        await AddMissingMelodies(missingInTarget, _targetDb, _sourceDb);

        _logger.LogInformation("Синхронізація мелодій завершена.");
    }

    private async Task AddMissingMelodies(IEnumerable<Melody> missingMelodies, DbContext targetDb, DbContext sourceDb)
    {
        foreach (var melody in missingMelodies)
        {
            if (melody.Author is null) continue;

            var normSurname = (melody.Author.Surname ?? "").Trim().ToLower();
            var author = await targetDb.Set<Author>()
                .Include(a => a.Country)
                .FirstOrDefaultAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname);

            if (author == null)
            {
                author = await sourceDb.Set<Author>()
                    .Include(a => a.Country)
                    .FirstOrDefaultAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname);
                    
                if (author != null && author.Country != null)
                {
                    var normCountry = (author.Country.Name ?? "").Trim().ToLower();
                    var country = await targetDb.Set<Country>()
                        .FirstOrDefaultAsync(c => ((c.Name ?? "").Trim().ToLower()) == normCountry);
                    if (country == null)
                    {
                        country = new Country { Name = author.Country.Name };
                        targetDb.Set<Country>().Add(country);
                        await targetDb.SaveChangesAsync();
                    }
                    var authorCopy = new Author
                    {
                        Name = author.Name,
                        Surname = author.Surname,
                        Country = country,
                        DateOfBirth = author.DateOfBirth,
                        DateOfDeath = author.DateOfDeath,
                        Description = author.Description
                    };
                    targetDb.Set<Author>().Add(authorCopy);
                    await targetDb.SaveChangesAsync();
                    author = authorCopy;
                }
            }

            if (author != null)
            {
                var normTitle = (melody.Title ?? "").Trim().ToLower();
                bool exists = await targetDb.Set<Melody>()
                    .AnyAsync(m => ((m.Title ?? "").Trim().ToLower()) == normTitle && m.AuthorID == author.ID);
                if (exists)
                {
                    AddCollision($"Melody '{melody.Title}' автора '{melody.Author.Surname}' вже існує у target (пропуск).");
                    continue;
                }

                var melodyCopy = new Melody
                {
                    Title = melody.Title,
                    Author = author,
                    Year = melody.Year,
                    Description = melody.Description,
                    FilePath = melody.FilePath,
                    IsFileEligible = melody.IsFileEligible,
                    Tonality = melody.Tonality,
                    AuthorID = author.ID
                };
                targetDb.Set<Melody>().Add(melodyCopy);
            }
        }
        await targetDb.SaveChangesAsync();
    }

    private static bool Same(string a, string b) =>
        string.Equals(a?.Trim(), b?.Trim(), StringComparison.OrdinalIgnoreCase);
}