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

    private bool _interactiveMode = false;
    public bool InteractiveMode 
    { 
        get => _interactiveMode; 
        set => _interactiveMode = value; 
    }

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

    //---------
    //синхронізація авторів
    //--------
    private async Task SyncAuthorsAsync()
    {
        // Reset IDs to avoid conflicts
        foreach (var a in _missingAuthorsInTarget) a.ID = 0;

        // Додати відсутні країни в цільову базу даних
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
            var normSurnameEn = (author.SurnameEn ?? "").Trim().ToLower();
            var normNameEn = (author.NameEn ?? "").Trim().ToLower();

            //surname and name match
            bool exists = await _targetDb.Author.AnyAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname && ((a.Name ?? "").Trim().ToLower()) == normName);

            //surname match only (possible duplicate)
            bool possibleDuplicate = await _targetDb.Author.AnyAsync(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname);

            bool enDuplicate = await _targetDb.Author.AnyAsync(a => ((a.SurnameEn ?? "").Trim().ToLower()) == normSurnameEn && ((a.NameEn ?? "").Trim().ToLower()) == normNameEn);

            // NEW: Check if all four fields match exactly (complete author match)
            bool completeMatch = await _targetDb.Author.AnyAsync(a => 
                ((a.Surname ?? "").Trim().ToLower()) == normSurname && 
                ((a.Name ?? "").Trim().ToLower()) == normName &&
                ((a.SurnameEn ?? "").Trim().ToLower()) == normSurnameEn && 
                ((a.NameEn ?? "").Trim().ToLower()) == normNameEn);

            if (completeMatch)
            {
                // Повний збіг - точно той самий автор, пропускаємо без питань
                _logger.LogInformation($"Author '{author.Surname} {author.Name}' / '{author.SurnameEn} {author.NameEn}' already exists (complete match - skipping).");
                continue;
            }

            if (exists || possibleDuplicate || enDuplicate)
            {
                // Часткові збіги - залежить від режиму
                if (_interactiveMode)
                {
                    var decision = await AskUserDecisionAsync(author, exists, possibleDuplicate, enDuplicate);
                    if (decision == UserDecision.Skip)
                    {
                        AddCollision($"Author '{author.Surname} {author.Name}' skipped by user decision.");
                        continue;
                    }
                    else if (decision == UserDecision.Add)
                    {
                        // Продовжити додавання автора попри конфлікт
                        _logger.LogInformation($"User decided to add author '{author.Surname} {author.Name}' despite potential duplicate.");
                    }
                    else if (decision == UserDecision.Merge)
                    {
                        // Об'єднати з існуючим автором
                        var mergedAuthor = await MergeAuthorsAsync(author);
                        _logger.LogInformation($"User decided to merge author '{author.Surname} {author.Name}' with existing author ID {mergedAuthor.ID}.");
                        continue; // Не додавати нового автора, використовувати об'єднаний
                    }
                }
                else
                {
                    // Автоматично пропустити (старий механізм)
                    AddCollision($"Author '{author.Surname} {author.Name}' already exists (skip).");
                    continue;
                }
            }

            _targetDb.Author.Add(new Author
            {
                Name = author.Name,
                Surname = author.Surname,
                NameEn = author.NameEn,
                SurnameEn = author.SurnameEn,
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

    // Enum для типів рішень користувача
    public enum UserDecision
    {
        Skip,
        Add,
        Merge,  // NEW: Об'єднати авторів
        AddWithModification
    }

    // Метод для інтерактивного запиту
    private async Task<UserDecision> AskUserDecisionAsync(Author author, bool exists, bool possibleDuplicate, bool enDuplicate)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== ПОТЕНЦІЙНИЙ КОНФЛІКТ АВТОРА ===");
  Console.ResetColor();
        
        Console.WriteLine($"Автор для синхронізації: {author.Surname} {author.Name} ({author.MelodiesCount} композицій)");
   if (!string.IsNullOrEmpty(author.SurnameEn) || !string.IsNullOrEmpty(author.NameEn))
       Console.WriteLine($"Англійське ім'я: {author.SurnameEn} {author.NameEn}");
     
  Console.WriteLine($"Країна: {author.Country?.Name}");
        Console.WriteLine($"Роки життя: {author.DateOfBirth}-{author.DateOfDeath}");
        
 // NEW: Показати існуючих авторів у цільовій базі
        await ShowExistingAuthorsAsync(author);
     
        Console.WriteLine();
     Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("ВИЯВЛЕНО КОНФЛІКТИ:");
        Console.ResetColor();
        
        if (exists)
        Console.WriteLine("• Точний збіг імені та прізвища");
        if (possibleDuplicate)
            Console.WriteLine("• Збіг прізвища (можливий дублікат)");
        if (enDuplicate)
     Console.WriteLine("• Збіг англійського імені та прізвища");

        Console.WriteLine();
        Console.WriteLine("Оберіть дію:");
        Console.WriteLine("1 - Пропустити цього автора (безпечно)");
        Console.WriteLine("2 - Додати попри конфлікт (може створити дублікат)");
        Console.WriteLine("3 - Об'єднати з існуючим автором (merge)");  // NEW
        Console.WriteLine("0 - Відмінити синхронізацію");
        Console.Write("Ваш вибір (1/2/3/0): ");

        while (true)
     {
 var input = Console.ReadLine()?.Trim();
  switch (input)
            {
      case "1":
   Console.WriteLine("✓ Автор буде пропущений.");
      return UserDecision.Skip;
    case "2":
        Console.ForegroundColor = ConsoleColor.Red;
  Console.WriteLine("⚠ УВАГА: Автор буде доданий попри можливий конфлікт!");
        Console.ResetColor();
          return UserDecision.Add;
 case "3":// NEW
  Console.ForegroundColor = ConsoleColor.Cyan;
           Console.WriteLine("🔄 Автор буде об'єднаний з існуючим.");
           Console.ResetColor();
        return UserDecision.Merge;
     case "0":
     Console.WriteLine("Синхронізація відмінена користувачем.");
               throw new OperationCanceledException("User cancelled synchronization");
          default:
         Console.Write("Некоректний вибір. Введіть 1, 2, 3 або 0: ");
     break;
  }
     }
    }

    // NEW: Метод для показу існуючих авторів
    private async Task ShowExistingAuthorsAsync(Author sourceAuthor)
    {
        var normSurname = (sourceAuthor.Surname ?? "").Trim().ToLower();
        
   var existingAuthors = await _targetDb.Author
            .Include(a => a.Country)
     .Where(a => 
          ((a.Surname ?? "").Trim().ToLower()) == normSurname ||
     ((a.Surname ?? "").Trim().ToLower()).Contains(normSurname.Substring(0, Math.Min(3, normSurname.Length))))
            .ToListAsync();
        
        if (existingAuthors.Any())
        {
         Console.WriteLine();
   Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("ІСНУЮЧІ АВТОРИ У ЦІЛЬОВІЙ БАЗІ:");
   Console.ResetColor();
            
 for (int i = 0; i < existingAuthors.Count; i++)
            {
                var existing = existingAuthors[i];
       Console.WriteLine($"  [{i + 1}] {existing.Surname} {existing.Name} | {existing.SurnameEn} {existing.NameEn}");
  Console.WriteLine($"    Країна: {existing.Country?.Name} | Роки: {existing.DateOfBirth}-{existing.DateOfDeath}");
            }
        }
    }

    // NEW: Метод для об'єднання авторів
    private async Task<Author> MergeAuthorsAsync(Author sourceAuthor)
    {
        var normSurname = (sourceAuthor.Surname ?? "").Trim().ToLower();
        var normName = (sourceAuthor.Name ?? "").Trim().ToLower();
        
   // Знайти найбільш схожого автора для об'єднання
        var targetAuthor = await _targetDb.Author
   .Include(a => a.Country)
            .Where(a => 
        ((a.Surname ?? "").Trim().ToLower()) == normSurname &&
        ((a.Name ?? "").Trim().ToLower()) == normName)
            .FirstOrDefaultAsync();
        
 // Якщо точного збігу немає, шукати по прізвищу
        if (targetAuthor == null)
        {
            var possibleAuthors = await _targetDb.Author
   .Include(a => a.Country)
         .Where(a => ((a.Surname ?? "").Trim().ToLower()) == normSurname)
        .ToListAsync();
 
        if (possibleAuthors.Count == 1)
      {
   targetAuthor = possibleAuthors[0];
            }
         else if (possibleAuthors.Count > 1)
    {
                targetAuthor = await AskUserToChooseAuthorAsync(possibleAuthors);
       }
        }
        
 if (targetAuthor == null)
        {
            throw new InvalidOperationException("Не вдалося знайти автора для об'єднання.");
        }
  
        // Виконати merge полів
        bool hasChanges = false;
        
     // Об'єднання англійських імен
        if (string.IsNullOrWhiteSpace(targetAuthor.NameEn) && !string.IsNullOrWhiteSpace(sourceAuthor.NameEn))
        {
      targetAuthor.NameEn = sourceAuthor.NameEn;
            hasChanges = true;
        }
   
 if (string.IsNullOrWhiteSpace(targetAuthor.SurnameEn) && !string.IsNullOrWhiteSpace(sourceAuthor.SurnameEn))
        {
            targetAuthor.SurnameEn = sourceAuthor.SurnameEn;
  hasChanges = true;
        }
        
        // Об'єднання дат народження/смерті
        if (targetAuthor.DateOfBirth == null && sourceAuthor.DateOfBirth != null)
    {
targetAuthor.DateOfBirth = sourceAuthor.DateOfBirth;
            hasChanges = true;
  }
  
   if (targetAuthor.DateOfDeath == null && sourceAuthor.DateOfDeath != null)
    {
       targetAuthor.DateOfDeath = sourceAuthor.DateOfDeath;
         hasChanges = true;
        }
  
        // Об'єднання опису
    if (string.IsNullOrWhiteSpace(targetAuthor.Description) && !string.IsNullOrWhiteSpace(sourceAuthor.Description))
        {
         targetAuthor.Description = sourceAuthor.Description;
            hasChanges = true;
  }
        else if (!string.IsNullOrWhiteSpace(targetAuthor.Description) && !string.IsNullOrWhiteSpace(sourceAuthor.Description))
        {
            // Об'єднати описи якщо вони різні
if (!targetAuthor.Description.Contains(sourceAuthor.Description))
    {
           targetAuthor.Description += "\n\n--- З джерельної бази ---\n" + sourceAuthor.Description;
       hasChanges = true;
         }
        }
        
 // Об'єднання країни
        if (targetAuthor.Country == null && sourceAuthor.Country != null)
        {
            var country = await _targetDb.Country.FirstOrDefaultAsync(c => 
      ((c.Name ?? "").Trim().ToLower()) == (sourceAuthor.Country.Name ?? "").Trim().ToLower());
  
            if (country == null)
      {
             country = new Country { Name = sourceAuthor.Country.Name };
     _targetDb.Country.Add(country);
     await _targetDb.SaveChangesAsync();
   }
         
   targetAuthor.Country = country;
          hasChanges = true;
   }
        
        if (hasChanges)
{
            await _targetDb.SaveChangesAsync();
       _logger.LogInformation($"Merged author '{sourceAuthor.Surname} {sourceAuthor.Name}' with existing author ID {targetAuthor.ID}.");
        }
  
        return targetAuthor;
    }

    // NEW: Метод для вибору автора з списку
    private async Task<Author> AskUserToChooseAuthorAsync(List<Author> authors)
    {
        Console.WriteLine();
        Console.WriteLine("Виберіть автора для об'єднання:");
        
        for (int i = 0; i < authors.Count; i++)
        {
            var author = authors[i];
     Console.WriteLine($"{i + 1} - {author.Surname} {author.Name} | {author.SurnameEn} {author.NameEn} (ID: {author.ID})");
            Console.WriteLine($"    Країна: {author.Country?.Name} | Роки: {author.DateOfBirth}-{author.DateOfDeath}");
        }
   
        Console.Write($"Введіть номер (1-{authors.Count}): ");
        
 while (true)
        {
            var input = Console.ReadLine()?.Trim();
            if (int.TryParse(input, out int choice) && choice >= 1 && choice <= authors.Count)
         {
     return authors[choice - 1];
       }
            Console.Write($"Некоректний вибір. Введіть номер від 1 до {authors.Count}: ");
        }
    }
}