using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseSyncService
{
    private readonly Melodies25Context _localDb;
    private readonly Melodies25SyncContext _sqlExpressDb;
    private readonly ILogger<DatabaseSyncService> _logger;
    private List<Author> missingAuthorsInLocal;
    private List<Author> missingAuthorsExpress;

    public DatabaseSyncService(Melodies25Context localDb, Melodies25SyncContext sqlExpressDb, ILogger<DatabaseSyncService> logger)
    {
        _localDb = localDb;
        _sqlExpressDb = sqlExpressDb;
        _logger = logger;
    }

    public async Task SyncDatabasesAsync()
    {
        _logger.LogInformation("Початок синхронізації баз даних...");

        // Завантажуємо всі країни
        var localCountries = await _localDb.Country.AsNoTracking().ToListAsync();
        var expressCountries = await _sqlExpressDb.Country.AsNoTracking().ToListAsync();

        // Знаходимо відсутні країни у локальній базі
        var missingCountriesInLocal = expressCountries.Where(ec => !localCountries.Any(lc => lc.Name == ec.Name)).ToList();
        if (missingCountriesInLocal.Any())
        {
            var countryNames = string.Join("\n", missingCountriesInLocal.Select(c => c.Name));
            _logger.LogInformation("Відсутні країни у локальній БД: {CountryNames}", countryNames);
        }
        else
            _logger.LogInformation("\nВідсутніх країн у локальній БД не виявлено");

        // Завантажуємо всіх авторів з прив'язаними країнами
        var localAuthors = await _localDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        var expressAuthors = await _sqlExpressDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();

        // Знаходимо відсутніх авторів у локальній базі
        missingAuthorsInLocal = expressAuthors.Where(ea => !localAuthors.Any(la => la.Name == ea.Name && la.Surname == ea.Surname)).ToList();
        
        if (missingAuthorsInLocal.Any())
        {
            var authorNames = string.Join("\n", missingAuthorsInLocal.Select(a => $"{a.Name} {a.Surname}"));
            _logger.LogInformation("\nВідсутні автори у локальній БД: {AuthorNames}", authorNames);
        }
        else
            _logger.LogInformation("\nВідсутніх авторів не виявлено");

        // Завантажуємо всі мелодії з прив'язаними авторами і країнами
        var localMelodies = await _localDb.Melody
            .AsNoTracking()
            .Include(m => m.Author)
                .ThenInclude(a => a.Country)
            .ToListAsync();

        var expressMelodies = await _sqlExpressDb.Melody
            .AsNoTracking()
            .Include(m => m.Author)
                .ThenInclude(a => a.Country)
            .ToListAsync();

        // Знаходимо відсутні мелодії у локальній базі
        var missingMelodiesInLocal = expressMelodies.Where(em => !localMelodies.Any(lm => lm.ID == em.ID)).ToList();
        if (missingMelodiesInLocal.Any())
        {
            var melodyTitles = string.Join(", ", missingMelodiesInLocal.Select(m => $"\n{m.Title} ({m.Author.Name} {m.Author.Surname})"));
            _logger.LogInformation("Відсутні мелодії у локальній БД: {MelodyTitles}", melodyTitles);
        }
        else
            _logger.LogInformation("\nВідсутніх мелодій не виявлено");

        _logger.LogInformation("Перевірка на відсутні записи завершена.");


        // 1. Оновлення країн
        await SyncCountries();

        // 2. Оновлення авторів
        await SyncAuthors();

        // 3. Оновлення мелодій
        await SyncMelodies();

        _logger.LogInformation("Синхронізація баз даних завершена.");
    }

    private async Task SyncCountries()
    {
        var localCountries = await _localDb.Country.AsNoTracking().ToListAsync();
        var expressCountries = await _sqlExpressDb.Country.AsNoTracking().ToListAsync();

        var missingInLocal = expressCountries.Where(ec => !localCountries.Any(lc => lc.Name == ec.Name)).ToList();
        var missingInExpress = localCountries.Where(lc => !expressCountries.Any(ec => ec.Name == lc.Name)).ToList();

        // Видаляємо явно вказані ID з нових записів
        foreach (var country in missingInLocal)
        {
            country.ID = 0; // Якщо ID - автоінкремент, скидаємо його на 0
        }

        foreach (var country in missingInExpress)
        {
            country.ID = 0; // Скидаємо ID на 0
        }

        if (missingInLocal.Any())
        {
            _localDb.Country.AddRange(missingInLocal);
            await _localDb.SaveChangesAsync();
        }

        if (missingInExpress.Any())
        {
            _sqlExpressDb.Country.AddRange(missingInExpress);
            await _sqlExpressDb.SaveChangesAsync();
        }

        _logger.LogInformation("\nСинхронізація країн завершена.\n");
    }


    private async Task SyncAuthors()
    {
        var localAuthors = await _localDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();
        var expressAuthors = await _sqlExpressDb.Author.AsNoTracking().Include(a => a.Country).ToListAsync();

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

        await AddMissingAuthors(missingInLocal, _localDb, _sqlExpressDb);
        await AddMissingAuthors(missingInExpress, _sqlExpressDb, _localDb);

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
        var localMelodies = await _localDb.Melody.AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        var expressMelodies = await _sqlExpressDb.Melody.AsNoTracking()
            .Include(m => m.Author).ThenInclude(a => a.Country)
            .ToListAsync();

        var missingInLocal = expressMelodies.Where(em => !localMelodies.Any(lm => lm.ID == em.ID)).ToList();
        var missingInExpress = localMelodies.Where(lm => !expressMelodies.Any(em => em.ID == lm.ID)).ToList();

        await AddMissingMelodies(missingInLocal, _localDb, _sqlExpressDb);
        await AddMissingMelodies(missingInExpress, _sqlExpressDb, _localDb);

        _logger.LogInformation("Синхронізація мелодій завершена.");
    }


    private async Task AddMissingMelodies(IEnumerable<Melody> missingMelodies, DbContext targetDb, DbContext sourceDb)
    {
        foreach (var melody in missingMelodies)
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

                if (author != null)
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

        await targetDb.SaveChangesAsync();
    }
}