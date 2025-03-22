using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseSyncService
{
    private readonly Melodies25Context _sourceDb;
    private readonly Melodies25TargetContext _targetDb;
    private readonly ILogger<DatabaseSyncService> _logger;
    private List<Author> missingAuthorsInLocal;
    private List<Country> missingCountriesInLocal;
    private List<Melody> missingMelodiesInSource;

    public DatabaseSyncService(Melodies25Context sourceDb, Melodies25TargetContext sqlTargetDb, ILogger<DatabaseSyncService> logger)
    {
        _sourceDb = sourceDb;
        _targetDb = sqlTargetDb;
        _logger = logger;
    }

    public async Task SyncDatabasesAsync()
    {
        _logger.LogInformation("Початок синхронізації баз даних...");


        // Знаходимо відсутні країни у цільовій базі
        bool ifmissingcountries = await AnalyzeCountries();

        // Завантажуємо всіх авторів з прив'язаними країнами
        bool ifmissingauthors = await AnalyzeAuthors();

        // Завантажуємо всі мелодії з прив'язаними авторами і країнами
        bool ifmissingmelodies = await AnalyzeMelodies();

        _logger.LogInformation("Перевірка на відсутні записи завершена.");


        // 1. Оновлення країн
        if (ifmissingcountries)
            await SyncCountries();
        else
            _logger.LogInformation("skip countries upd.");

        // 2. Оновлення авторів
        if (ifmissingauthors)
            await SyncAuthors();
        else
            _logger.LogInformation("skip authors upd.");

        // 3. Оновлення мелодій
        if (ifmissingmelodies)
            await SyncMelodies();
        else
            _logger.LogInformation("skip melodies upd.");

        _logger.LogInformation("Синхронізація баз даних завершена.");
    }

    private async Task<bool> AnalyzeMelodies()
    {
        bool ifmissingmelodies = false;

        var sourceMelodies = await _sourceDb.Melody
                    .AsNoTracking()
                    .Include(m => m.Author)
                        .ThenInclude(a => a.Country)
                    .ToListAsync();

        var targetMelodies = await _targetDb.Melody
            .AsNoTracking()
            .Include(m => m.Author)
                .ThenInclude(a => a.Country)
            .ToListAsync();

        _logger.LogInformation("\nМелодій у локальній БД: {MelodyCount}", sourceMelodies.Count());
        _logger.LogInformation("Мелодій у експрес БД: {MelodyCount}", targetMelodies.Count());

        // Знаходимо відсутні мелодії у локальній базі
        missingMelodiesInSource = targetMelodies.Where(em => !sourceMelodies.Any(lm => lm.Title == em.Title && lm.Author.Surname == em.Author.Surname)).ToList();
        if (missingMelodiesInSource.Any())
        {
            var melodyTitles = string.Join(", ", missingMelodiesInSource.Select(m => $"\n{m.Title} ({m.Author.Name} {m.Author.Surname})"));
            _logger.LogInformation("Відсутні мелодії у локальній БД: {MelodyTitles}", melodyTitles);
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
        var expressAuthors = await _targetDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();

        _logger.LogInformation("\nАвторів у локальній БД: {CountryCount}", localAuthors.Count());
        _logger.LogInformation("Авторів у експрес БД: {CountryCount}", expressAuthors.Count());

        // Знаходимо відсутніх авторів у локальній базі
        missingAuthorsInLocal = expressAuthors.Where(ea => !localAuthors.Any(la => la.Name == ea.Name && la.Surname == ea.Surname)).ToList();

        if (missingAuthorsInLocal.Any())
        {
            var authorNames = string.Join("\n", missingAuthorsInLocal.Select(a => $"{a.Name} {a.Surname}"));
            _logger.LogInformation("\nВідсутні автори у локальній БД: {AuthorNames}", authorNames);
            ifmissingauthors = true;
        }
        else
            _logger.LogInformation("\nВідсутніх авторів не виявлено");
        return ifmissingauthors;
    }

    private async Task<bool> AnalyzeCountries()
    {
        bool ifmissingcountries = false;
        var sourceCountries = await _sourceDb.Country.AsNoTracking().ToListAsync();
        var targetCountries = await _targetDb.Country.AsNoTracking().ToListAsync();
        _logger.LogInformation("\nКраїн у джерельній БД: {CountryCount}", sourceCountries.Count());
        _logger.LogInformation("Країн у цільовій БД: {CountryCount}", targetCountries.Count());

        missingCountriesInLocal = sourceCountries.Where(sc => !targetCountries.Any(tc => tc.Name == sc.Name)).ToList();
        if (missingCountriesInLocal.Any())
        {
            var countryNames = string.Join("\n", missingCountriesInLocal.Select(c => c.Name));
            _logger.LogInformation("\nВідсутні країни у локальній БД: {CountryNames}", countryNames);
            ifmissingcountries = true;
        }
        else
        {
            _logger.LogInformation("\nВідсутніх країн у локальній БД не виявлено");
        }
        return ifmissingcountries;
    }

    private async Task SyncCountries()
    {
        var sourceCountries = await _sourceDb.Country.AsNoTracking().ToListAsync();
        var targetCountries = await _targetDb.Country.AsNoTracking().ToListAsync();

        var missingInLocal = missingCountriesInLocal;
        var missingInTarget = targetCountries.Where(lc => !sourceCountries.Any(ec => ec.Name == lc.Name)).ToList();

        // Видаляємо явно вказані ID з нових записів
        foreach (var country in missingInLocal)
        {
            country.ID = 0; // Якщо ID - автоінкремент, скидаємо його на 0
            _logger.LogInformation($"відстня - {country.Name}");
        }

        foreach (var country in missingInTarget)
        {
            country.ID = 0; // Скидаємо ID на 0
        }

        if (missingInLocal.Any())
        {
            _sourceDb.Country.AddRange(missingInLocal);
            await _sourceDb.SaveChangesAsync();
        }

        if (missingInTarget.Any())
        {
            _targetDb.Country.AddRange(missingInTarget);
            await _targetDb.SaveChangesAsync();
        }

        _logger.LogInformation("\nСинхронізація країн завершена.\n");
    }


    private async Task SyncAuthors()
    {
        var localAuthors = await _sourceDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        var expressAuthors = await _targetDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();

        var missingInLocal = missingAuthorsInLocal;
        var missingInExpress = localAuthors.Where(la => !expressAuthors.Any(ea => ea.Name == la.Name && ea.Surname == ea.Surname)).ToList();


        // Скидаємо ID для нових авторів
        foreach (var author in missingInLocal)
        {
            author.ID = 0; // Якщо ID - автоінкремент, скидаємо його на 0
        }

        foreach (var author in missingInExpress)
        {
            author.ID = 0; // Скидаємо ID на 0
        }

        await AddMissingAuthors(missingInLocal, _sourceDb, _targetDb);
        //await AddMissingAuthors(missingInTarget, _targetDb, _sourceDb);

        _logger.LogInformation("\nСинхронізація авторів завершена.\n");
    }


    private async Task AddMissingAuthors(IEnumerable<Author> missingAuthors, DbContext targetDb, DbContext sourceDb)
    {
        foreach (var author in missingAuthors)
        {
            Country country = null;

            // Якщо у автора є країна, спробуємо знайти її в цільовій базі даних
            if (author.Country != null)
            {
                country = await targetDb.Set<Country>().FirstOrDefaultAsync(c => c.Name == author.Country.Name);

                // Якщо країна не знайдена в цільовій БД, шукаємо її в джерелі
                if (country == null)
                {
                    country = await sourceDb.Set<Country>().FirstOrDefaultAsync(c => c.Name == author.Country.Name);

                    // Якщо країна знайдена в джерелі, додаємо її до цільової БД
                    if (country != null)
                    {
                        targetDb.Set<Country>().Add(country);
                        await targetDb.SaveChangesAsync();  // Зберігаємо зміни в цільовій базі
                    }
                }
            }

            // Створюємо копію автора для додавання в цільову БД
            var authorCopy = new Author
            {
                Name = author.Name,
                Surname = author.Surname,
                Country = country, // Якщо країна є, то додається, якщо ні, то null
                DateOfBirth = author.DateOfBirth,
                DateOfDeath = author.DateOfDeath,
                Description = author.Description
            };

            // Додаємо автора в цільову БД
            targetDb.Set<Author>().Add(authorCopy);

            _logger.LogInformation("спроба додати автора");
        }

        // Зберігаємо всі зміни
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
        var missingInTarget = sourceMelodies.Where(lm => !targetMelodies.Any(em => em.Title == lm.Title && em.Author.Surname == em.Author.Surname)).ToList();
        _logger.LogInformation($"missing in source - {missingInSource.Count}.");
        _logger.LogInformation($"missing in target - {missingInTarget.Count}.");


        _logger.LogInformation("Start copying to target db.");

        await AddMissingMelodies(missingInSource, _sourceDb, _targetDb);
        await AddMissingMelodies(missingInTarget, _targetDb, _sourceDb);

        _logger.LogInformation("Синхронізація мелодій завершена.");
    }


    private static async Task AddMissingMelodies(IEnumerable<Melody> missingMelodies, DbContext targetDb, DbContext sourceDb)
    {
        foreach (var melody in missingMelodies)
        {
            if (melody.Author is not null)
            {
                var author = await targetDb.Set<Author>()
                    .Include(a => a.Country)
                    .FirstOrDefaultAsync(a => a.Surname == melody.Author.Surname);

                if (author == null)
                {
                    // Спробуємо знайти автора у вихідній БД
                    author = await sourceDb.Set<Author>()
                        .Include(a => a.Country)
                        .FirstOrDefaultAsync(a => a.Surname == melody.Author.Surname);

                    if (author != null && author.Country != null)
                    {
                        // Переконаємося, що країна існує у цільовій БД
                        var country = await targetDb.Set<Country>().FirstOrDefaultAsync(c => c.Name == author.Country.Name);
                        if (country == null)
                        {
                            country = new Country { Name = author.Country.Name };
                            targetDb.Set<Country>().Add(country);
                            await targetDb.SaveChangesAsync();
                        }

                        // Додаємо автора у цільову БД
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
                    var melodyCopy = new Melody
                    {
                        Title = melody.Title,
                        Author = author, // Автор може бути вже асоційований, тому можна просто передати його
                        Year = melody.Year,
                        Description = melody.Description,
                        Filepath = melody.Filepath,
                        IsFileEligible = melody.IsFileEligible,
                        Tonality = melody.Tonality,
                        AuthorID = melody.AuthorID,
                        //ID = melody.ID // Якщо ID є автоінкрементним, цей рядок можна прибрати
                    };

                    targetDb.Set<Melody>().Add(melodyCopy);
                }
            }
        }

        await targetDb.SaveChangesAsync();
    }
}