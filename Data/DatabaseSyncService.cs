using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;

public class DatabaseSyncService
{
    private readonly Melodies25Context _localDb;
    private readonly Melodies25SyncContext _sqlExpressDb;
    private readonly ILogger<DatabaseSyncService> _logger;

    public DatabaseSyncService(Melodies25Context localDb, Melodies25SyncContext sqlExpressDb, ILogger<DatabaseSyncService> logger)
    {
        _localDb = localDb;
        _sqlExpressDb = sqlExpressDb;
        _logger = logger;
    }

    public async Task SyncDatabasesAsync()
    {
        _logger.LogInformation("Початок синхронізації баз даних...");

        var localMelodies = await _localDb.Melody.AsNoTracking().Include(m => m.Author).ThenInclude(a => a.Country).ToListAsync();
        var expressMelodies = await _sqlExpressDb.Melody.AsNoTracking().Include(m => m.Author).ThenInclude(a => a.Country).ToListAsync();

        var missingInExpress = localMelodies.Where(lm => !expressMelodies.Any(em => em.ID == lm.ID)).ToList();
        var missingInLocal = expressMelodies.Where(em => !localMelodies.Any(lm => lm.ID == em.ID)).ToList();
        var updatedInExpress = expressMelodies.Where(em => localMelodies.Any(lm => lm.ID == em.ID && !lm.Equals(em))).ToList();
        var updatedInLocal = localMelodies.Where(lm => expressMelodies.Any(em => em.ID == lm.ID && !em.Equals(lm))).ToList();

        _logger.LogInformation("Перевірка на відсутні записи у SQLExpress та локальній БД завершена.");

        // Додаємо нові записи в SQLExpress та локальну БД
        await AddMissingMelodies(missingInExpress, IfCountryExistsSQL, IfAuthorExistsSQL, _sqlExpressDb);
        await AddMissingMelodies(missingInLocal, IfCountryExistsLocal, IfAuthorExistsLocal, _localDb);

        // Оновлюємо змінені записи
        if (updatedInExpress.Any())
        {
            _localDb.Melody.UpdateRange(updatedInExpress);
            await _localDb.SaveChangesAsync();
        }
        if (updatedInLocal.Any())
        {
            _sqlExpressDb.Melody.UpdateRange(updatedInLocal);
            await _sqlExpressDb.SaveChangesAsync();
        }

        _logger.LogInformation("Синхронізація баз даних завершена.");
    }

    private async Task AddMissingMelodies(IEnumerable<Melody> missingMelodies, Func<Melody, Task> checkCountry, Func<Melody, Task> checkAuthor, DbContext dbContext)
    {
        if (missingMelodies.Any())
        {
            foreach (var melody in missingMelodies)
            {
                // Перевірка на наявність країни та автора
                await checkCountry(melody);
                await checkAuthor(melody);

                // Додаємо мелодії без вказівки ID (для автоінкременту)
                melody.ID = 0; // Встановлюємо ID на 0, щоб дозволити автоінкремент
                dbContext.Set<Melody>().Add(melody);
            }

            await dbContext.SaveChangesAsync();
        }
    }

    private async Task IfCountryExistsLocal(Melody melody)
    {
        await IfCountryExists(melody, _localDb.Country, _sqlExpressDb.Country, _localDb);
    }

    private async Task IfCountryExistsSQL(Melody melody)
    {
        await IfCountryExists(melody, _sqlExpressDb.Country, _localDb.Country, _sqlExpressDb);
    }

    private async Task IfCountryExists(Melody melody, DbSet<Country> localSet, DbSet<Country> externalSet, DbContext dbContext)
    {
        if (melody.Author?.Country != null && !await localSet.AnyAsync(c => c.Name == melody.Author.Country.Name))
        {
            var country = await externalSet.FirstOrDefaultAsync(c => c.Name == melody.Author.Country.Name);
            if (country != null)
            {
                var countryCopy = new Country { Name = country.Name }; // No need to set the ID here
                localSet.Add(countryCopy);
                await dbContext.SaveChangesAsync(); // Let the database auto-generate the ID
            }
            else
            {
                _logger.LogWarning($"Країна з назвою {melody.Author?.Country?.Name} не знайдена.");
            }
        }
    }



    private async Task IfAuthorExistsLocal(Melody melody)
    {
        await IfAuthorExists(melody, _localDb.Author, _sqlExpressDb.Author, _localDb);
    }

    private async Task IfAuthorExistsSQL(Melody melody)
    {
        await IfAuthorExists(melody, _sqlExpressDb.Author, _localDb.Author, _sqlExpressDb);
    }

    private async Task IfAuthorExists(Melody melody, DbSet<Author> localSet, DbSet<Author> externalSet, DbContext dbContext)
    {
        if (!localSet.Any(a => a.ID == melody.AuthorID))
        {
            var author = await externalSet.FirstOrDefaultAsync(a => a.ID == melody.AuthorID);
            if (author != null)
            {
                var authorCopy = new Author
                {
                    Surname = author.Surname,
                    Name = author.Name,
                    SurnameEn = author.SurnameEn,
                    NameEn = author.NameEn,
                    Country = author.Country, // Прив'язуємо країну
                    DateOfBirth = author.DateOfBirth,
                    DateOfDeath = author.DateOfDeath,
                    Description = author.Description
                };
                localSet.Add(authorCopy);
                await dbContext.SaveChangesAsync(); // ID буде присвоєно автоматично
            }
            else
            {
                _logger.LogWarning($"Автор з ID {melody.AuthorID} не знайдений.");
            }
        }
    }
}
