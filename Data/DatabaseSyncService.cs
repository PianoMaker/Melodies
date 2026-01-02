using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Melodies25.Data;
using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;

public class DatabaseSyncService
{
    private readonly Melodies25SourceContext _sourceDb;   // VisualStudio (source)
    private readonly Melodies25TargetContext _targetDb;   // SQLExpress (target)
    private readonly ILogger<DatabaseSyncService> _logger;
    private readonly IWebHostEnvironment _environment;  // NEW: Для роботи з файлами

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

    public DatabaseSyncService(Melodies25SourceContext sourceDb, Melodies25TargetContext targetDb, ILogger<DatabaseSyncService> logger, IWebHostEnvironment environment)
    {
        _sourceDb = sourceDb;
        _targetDb = targetDb;
        _logger = logger;
        _environment = environment;
    }

    public async Task<IReadOnlyList<string>> SyncDatabasesAsync()
    {
        _collisions.Clear();
        _logger.LogInformation("Starting one-way sync SOURCE -> TARGET ...");

        bool countries = await AnalyzeCountriesAsync();
        bool authors = await AnalyzeAuthorsAsync();
        bool melodies = await AnalyzeMelodiesAsync();

        //КРАЇНИ
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Синхронізуємо країни");
        Console.ResetColor();
        if (countries) await SyncCountriesAsync(); else _logger.LogInformation("Countries up-to-date.");

        // АВТОРИ
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Синшронізуємо авторів");
        Console.ResetColor();
        if (authors) await SyncAuthorsAsync(); else _logger.LogInformation("Authors up-to-date.");

        // АВТОРИ-ФОТО
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Синхронізуємо фото");
        Console.ResetColor();
        await SyncMissingAuthorPhotosAsync();

        // АВТОРИ-АНГЛІЙСЬКІ ІМЕНА
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Синхронізуємо мультимовнімена");
        Console.ResetColor();
        await SyncAuthorEnglishNamesAsync();

        // МЕЛОДІЇ
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Синхронізуємо мелодії");
        Console.ResetColor();
        if (melodies) await SyncMelodiesAsync(); else _logger.LogInformation("Melodies up-to-date.");

        // NEW: Синхронізація MIDI файлів
        await SyncMidiFilesAsync();

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
        try
        {
            var src = await _sourceDb.Country.AsNoTracking().ToListAsync();
            var trg = await _targetDb.Country.AsNoTracking().ToListAsync();
            _missingCountriesInTarget = src.Where(sc => !trg.Any(tc => Same(tc.Name, sc.Name))).ToList();
            _logger.LogInformation("Missing countries in target: {Count}", _missingCountriesInTarget.Count);
            return _missingCountriesInTarget.Any();
        }
        catch (Exception ex)
        {
            // Якщо при зчитуванні Country сталася помилка (наприклад, відсутній стовпець `FlagUrl`),
            // повторимо запит з проєкцією, що не містить цього поля, щоб продовжити синхронізацію.
            _logger.LogWarning(ex, "Failed to read Country entities normally - retrying with lightweight projection.");

            var src = await _sourceDb.Country.AsNoTracking()
                .Select(c => new Country { ID = c.ID, Name = c.Name, NameEn = c.NameEn })
                .ToListAsync();

            var trg = await _targetDb.Country.AsNoTracking()
                .Select(c => new Country { ID = c.ID, Name = c.Name, NameEn = c.NameEn })
                .ToListAsync();

            _missingCountriesInTarget = src.Where(sc => !trg.Any(tc => Same(tc.Name, sc.Name))).ToList();
            _logger.LogInformation("Missing countries in target (fallback): {Count}", _missingCountriesInTarget.Count);
            return _missingCountriesInTarget.Any();
        }
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
                foreach (var srcCountry in toAdd)
                {

                    _targetDb.Country.Add(new Country
                    {
                        Name = srcCountry.Name,
                        NameEn = srcCountry.NameEn,
                        FlagUrl = srcCountry.FlagUrl
                    });
                    await TryCopyImageForEntryAsync(srcCountry.FlagUrl, "flags");
                }

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

            // Нормалізовані значення для перевірки
            var normSurname = (author.Surname ?? "").Trim().ToLower();
            var normName = (author.Name ?? "").Trim().ToLower();
            var normSurnameEn = (author.SurnameEn ?? "").Trim().ToLower();
            var normNameEn = (author.NameEn ?? "").Trim().ToLower();

            // 1. Перевірка точного збігу (ім'я + прізвище)
            bool exactMatch = await _targetDb.Author.AnyAsync(a =>
                ((a.Surname ?? "").Trim().ToLower()) == normSurname &&
                ((a.Name ?? "").Trim().ToLower()) == normName);

            // 2. Перевірка збігу англійських імен
            bool englishMatch = !string.IsNullOrEmpty(normSurnameEn) &&
                await _targetDb.Author.AnyAsync(a =>
                    ((a.SurnameEn ?? "").Trim().ToLower()) == normSurnameEn &&
                    ((a.NameEn ?? "").Trim().ToLower()) == normNameEn);

            // 3. Перевірка змішаних збігів (Surname = SurnameEn або навпаки)
            bool crossLanguageMatch = false;
            string crossMatchDetails = "";

            if (!string.IsNullOrEmpty(normSurname) && !string.IsNullOrEmpty(normSurnameEn))
            {
                // Перевірка чи Surname джерела = SurnameEn цілі
                bool surnameMatchesTargetEn = await _targetDb.Author.AnyAsync(a =>
                    ((a.SurnameEn ?? "").Trim().ToLower()) == normSurname);

                // Перевірка чи SurnameEn джерела = Surname цілі  
                bool surnameEnMatchesTarget = await _targetDb.Author.AnyAsync(a =>
                    ((a.Surname ?? "").Trim().ToLower()) == normSurnameEn);

                if (surnameMatchesTargetEn || surnameEnMatchesTarget)
                {
                    crossLanguageMatch = true;
                    crossMatchDetails = surnameMatchesTargetEn ?
                        $"Surname '{author.Surname}' matches existing SurnameEn" :
                        $"SurnameEn '{author.SurnameEn}' matches existing Surname";
                }
            }

            // 4. Перевірка часткових збігів по прізвищу
            bool partialMatch = await _targetDb.Author.AnyAsync(a =>
                ((a.Surname ?? "").Trim().ToLower()) == normSurname);

            // 5. Обробка результатів перевірки
            if (exactMatch)
            {
                AddCollision($"Author '{author.Surname} {author.Name}' already exists (exact match - skip).");
                continue;
            }

            if (englishMatch)
            {
                AddCollision($"Author '{author.SurnameEn} {author.NameEn}' already exists in English (skip).");
                continue;
            }

            if (crossLanguageMatch)
            {
                AddCollision($"Author '{author.Surname} {author.Name}' / '{author.SurnameEn} {author.NameEn}' - {crossMatchDetails} (NEEDS CONFIRMATION).");

                if (_interactiveMode)
                {
                    var decision = await AskCrossLanguageDecisionAsync(author, crossMatchDetails);
                    if (decision == UserDecision.Skip)
                    {
                        continue;
                    }
                    else if (decision == UserDecision.Merge)
                    {
                        await MergeCrossLanguageAuthorAsync(author);
                        continue;
                    }
                    // Якщо Add - продовжити додавання
                }
                else
                {
                    // В автоматичному режимі пропускати такі випадки
                    continue;
                }
            }

            if (partialMatch && _interactiveMode)
            {
                var decision = await AskUserDecisionAsync(author, false, true, false);
                if (decision == UserDecision.Skip)
                {
                    AddCollision($"Author '{author.Surname} {author.Name}' skipped by user decision.");
                    continue;
                }
            }

            // Додати автора якщо немає конфліктів
            var newAuthor = new Author
            {
                Name = author.Name,
                Surname = author.Surname,
                NameEn = author.NameEn,
                SurnameEn = author.SurnameEn,
                Country = country,
                DateOfBirth = author.DateOfBirth,
                DateOfDeath = author.DateOfDeath,
                Description = author.Description,
                DescriptionEn = author.DescriptionEn,
                Photo = author.Photo
            };

            _targetDb.Author.Add(newAuthor);

            // Спробувати скопіювати фізичний файл фото (якщо локальний шлях/ім'я)
            await TryCopyImageForEntryAsync(author.Photo, "authors");
        }

        await _targetDb.SaveChangesAsync();
        _logger.LogInformation("Authors sync complete.");
    }

    // NEW: Метод для обробки міжмовних конфліктів
    private async Task<UserDecision> AskCrossLanguageDecisionAsync(Author author, string details)
    {
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("=== МІЖМОВНИЙ КОНФЛІКТ АВТОРА ===");
        Console.ResetColor();

        Console.WriteLine($"Автор: {author.Surname} {author.Name}");
        Console.WriteLine($"English: {author.SurnameEn} {author.NameEn}");
        Console.WriteLine($"Конфлікт: {details}");

        // Показати схожих авторів
        await ShowSimilarAuthorsAsync(author);

        Console.WriteLine();
        Console.WriteLine("Це може бути той самий автор з різним написанням імені.");
        Console.WriteLine("Оберіть дію:");
        Console.WriteLine("1 - Пропустити (безпечно)");
        Console.WriteLine("2 - Додати як нового автора");
        Console.WriteLine("3 - Об'єднати з існуючим автором");
        Console.WriteLine("0 - Відмінити синхронізацію");
        Console.Write("Ваш вибір (1/2/3/0): ");

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1": return UserDecision.Skip;
                case "2": return UserDecision.Add;
                case "3": return UserDecision.Merge;
                case "0": throw new OperationCanceledException("User cancelled synchronization");
                default:
                    Console.Write("Некоректний вибір. Введіть 1, 2, 3 або 0: ");
                    break;
            }
        }
    }

    // NEW: Показати схожих авторів для міжмовного конфлікту
    private async Task ShowSimilarAuthorsAsync(Author sourceAuthor)
    {
        var normSurname = (sourceAuthor.Surname ?? "").Trim().ToLower();
        var normSurnameEn = (sourceAuthor.SurnameEn ?? "").Trim().ToLower();

        var similarAuthors = await _targetDb.Author
            .Include(a => a.Country)
            .Where(a =>
                ((a.Surname ?? "").Trim().ToLower()) == normSurname ||
                ((a.SurnameEn ?? "").Trim().ToLower()) == normSurname ||
                ((a.Surname ?? "").Trim().ToLower()) == normSurnameEn ||
                ((a.SurnameEn ?? "").Trim().ToLower()) == normSurnameEn)
            .ToListAsync();

        if (similarAuthors.Any())
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("СХОЖІ АВТОРИ У ЦІЛЬОВІЙ БАЗІ:");
            Console.ResetColor();

            foreach (var existing in similarAuthors)
            {
                Console.WriteLine($"  • {existing.Surname} {existing.Name} | {existing.SurnameEn} {existing.NameEn}");
                Console.WriteLine($"    Країна: {existing.Country?.Name} | Роки: {existing.DateOfBirth}-{existing.DateOfDeath}");
            }
        }
    }

    // NEW: Об'єднання авторів при міжмовному конфлікті  
    private async Task MergeCrossLanguageAuthorAsync(Author sourceAuthor)
    {
        var normSurname = (sourceAuthor.Surname ?? "").Trim().ToLower();
        var normSurnameEn = (sourceAuthor.SurnameEn ?? "").Trim().ToLower();

        // Знайти цільового автора для об'єднання
        var targetAuthor = await _targetDb.Author
            .Include(a => a.Country)
            .Where(a =>
                ((a.Surname ?? "").Trim().ToLower()) == normSurname ||
                ((a.SurnameEn ?? "").Trim().ToLower()) == normSurname ||
                ((a.Surname ?? "").Trim().ToLower()) == normSurnameEn ||
                ((a.SurnameEn ?? "").Trim().ToLower()) == normSurnameEn)
            .FirstOrDefaultAsync();

        if (targetAuthor == null)
        {
            throw new InvalidOperationException("Не вдалося знайти автора для міжмовного об'єднання.");
        }

        // Об'єднати поля
        bool hasChanges = await MergeAuthorFieldsAsync(targetAuthor, sourceAuthor);

        if (hasChanges)
        {
            await _targetDb.SaveChangesAsync();
            _logger.LogInformation($"Cross-language merged author '{sourceAuthor.Surname} {sourceAuthor.Name}' with existing author ID {targetAuthor.ID}.");
        }

        // Якщо фото перенеслося до цільового запису — скопіювати файл
        if (!string.IsNullOrWhiteSpace(sourceAuthor.Photo))
        {
            await TryCopyImageForEntryAsync(sourceAuthor.Photo, "authors");
        }
    }

    // NEW: Допоміжний метод для об'єднання полів авторів
    private async Task<bool> MergeAuthorFieldsAsync(Author targetAuthor, Author sourceAuthor)
    {
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

        // Об'єднання українських імен якщо їх немає
        if (string.IsNullOrWhiteSpace(targetAuthor.Name) && !string.IsNullOrWhiteSpace(sourceAuthor.Name))
        {
            targetAuthor.Name = sourceAuthor.Name;
            hasChanges = true;
        }

        if (string.IsNullOrWhiteSpace(targetAuthor.Surname) && !string.IsNullOrWhiteSpace(sourceAuthor.Surname))
        {
            targetAuthor.Surname = sourceAuthor.Surname;
            hasChanges = true;
        }

        // Інші поля...
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

        // Опис
        if (string.IsNullOrWhiteSpace(targetAuthor.Description) && !string.IsNullOrWhiteSpace(sourceAuthor.Description))
        {
            targetAuthor.Description = sourceAuthor.Description;
            hasChanges = true;
        }
        else if (!string.IsNullOrWhiteSpace(targetAuthor.Description) && !string.IsNullOrWhiteSpace(sourceAuthor.Description))
        {
            if (!targetAuthor.Description.Contains(sourceAuthor.Description))
            {
                targetAuthor.Description += "\n\n--- З джерельної бази ---\n" + sourceAuthor.Description;
                hasChanges = true;
            }
        }

        // Опис англійською
        if (string.IsNullOrWhiteSpace(targetAuthor.DescriptionEn) && !string.IsNullOrWhiteSpace(sourceAuthor.DescriptionEn))
        {
            targetAuthor.DescriptionEn = sourceAuthor.DescriptionEn;
            hasChanges = true;
        }
        else if (!string.IsNullOrWhiteSpace(targetAuthor.DescriptionEn) && !string.IsNullOrWhiteSpace(sourceAuthor.DescriptionEn))
        {
            if (!targetAuthor.DescriptionEn.Contains(sourceAuthor.DescriptionEn))
            {
                targetAuthor.DescriptionEn += "\n\n--- From source DB ---\n" + sourceAuthor.DescriptionEn;
                hasChanges = true;
            }
        }

        // Фото
        if (string.IsNullOrWhiteSpace(targetAuthor.Photo) && !string.IsNullOrWhiteSpace(sourceAuthor.Photo))
        {
            targetAuthor.Photo = sourceAuthor.Photo;
            hasChanges = true;
        }

        return hasChanges;
    }

    //---------
    //Синхронізація мелодій
    //--------
    private async Task SyncMelodiesAsync()
    {
        _logger.LogInformation("Starting melodies sync (DB-level)...");

        var melodiesPath = Path.Combine(_environment.WebRootPath, "melodies");
        if (!Directory.Exists(melodiesPath)) Directory.CreateDirectory(melodiesPath);

        // Load source and target melodies into memory (including author)
        var sourceMelodies = await _sourceDb.Melody.Include(m => m.Author).ToListAsync();
        var targetMelodies = await _targetDb.Melody.Include(m => m.Author).ToListAsync();

        string NormalizeKey(string? p) => (p ?? "").Replace('\\', '/').TrimStart('/').ToLowerInvariant();
        string NormalizeTitle(string? t) => (t ?? "").Trim().ToLowerInvariant();

        // Build lookup by normalized file path
        var srcByFile = sourceMelodies
            .Where(m => !string.IsNullOrWhiteSpace(m.FilePath))
            .GroupBy(m => NormalizeKey(m.FilePath))
            .ToDictionary(g => g.Key, g => g.ToList());

        var trgByFile = targetMelodies
            .Where(m => !string.IsNullOrWhiteSpace(m.FilePath))
            .GroupBy(m => NormalizeKey(m.FilePath))
            .ToDictionary(g => g.Key, g => g.ToList());

        //1) Find unique in source: those that have no matching title in target AND no matching file in target
        var targetTitlesSet = new HashSet<string>(targetMelodies.Select(t => NormalizeTitle(t.Title)), StringComparer.OrdinalIgnoreCase);

        List<Melody> uniqueSourceMelodies = GetUniqueSourceMelodies(sourceMelodies, trgByFile, targetTitlesSet);

        _logger.LogInformation("Unique source melodies to add: {Count}", uniqueSourceMelodies.Count);

        // If there are unique melodies, ask user for confirmation in interactive mode
        bool shouldAddUnique = AskIfAddUniqueMelodies(uniqueSourceMelodies);

        // Copy unique source melodies (both DB record and physical MIDI file) if confirmed
        if (shouldAddUnique && uniqueSourceMelodies.Count > 0)
        {
            await PrepareMelodiesForCopying(melodiesPath, uniqueSourceMelodies);
                        
            await _targetDb.SaveChangesAsync();
        }
        else
        {
            _logger.LogInformation("Skipping adding unique melodies (user declined or none). Pending conflict handling will continue.");
        }

        // Reload lists to reflect added records
        sourceMelodies = await _sourceDb.Melody.Include(m => m.Author).ToListAsync();
        targetMelodies = await _targetDb.Melody.Include(m => m.Author).ToListAsync();

        srcByFile = sourceMelodies
            .Where(m => !string.IsNullOrWhiteSpace(m.FilePath))
            .GroupBy(m => NormalizeKey(m.FilePath))
            .ToDictionary(g => g.Key, g => g.ToList());

        trgByFile = targetMelodies
            .Where(m => !string.IsNullOrWhiteSpace(m.FilePath))
            .GroupBy(m => NormalizeKey(m.FilePath))
            .ToDictionary(g => g.Key, g => g.ToList());

        //2) Handle files that are present in both DBs
        var filesInBoth = srcByFile.Keys.Intersect(trgByFile.Keys).ToList();

        foreach (var fileKey in filesInBoth)
        {
            var srcList = srcByFile[fileKey];
            var trgList = trgByFile[fileKey];

            var allTitles = srcList.Select(s => s.Title ?? "").Concat(trgList.Select(t => t.Title ?? ""))
                .Select(t => (t ?? "").Trim())
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            // Case A: Different titles for same file
            if (allTitles.Count > 1)
            {
                if (!_interactiveMode)
                {
                    AddCollision($"Title conflict for MIDI '{fileKey}' - multiple titles found; left unchanged (non-interactive).");
                }
                else
                {
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine("=== TITLE CONFLICT FOR MIDI FILE ===");
                    Console.ResetColor();
                    Console.WriteLine($"File: {fileKey}");

                    // List options
                    for (int i = 0; i < allTitles.Count; i++)
                    {
                        Console.WriteLine($"{i + 1} - {allTitles[i]}");
                    }
                    Console.WriteLine("0 - Keep all entries (do nothing)");
                    Console.Write("Choose title to keep (number) or 0: ");

                    while (true)
                    {
                        var input = Console.ReadLine()?.Trim();
                        if (int.TryParse(input, out int choice))
                        {
                            if (choice == 0) break; // keep all
                            if (choice >= 1 && choice <= allTitles.Count)
                            {
                                var chosenTitle = allTitles[choice - 1];

                                // Update target records: pick one target entity with chosen title if exists; remove other target entries for this file
                                var targetEntities = (await _targetDb.Melody.Where(m => m.FilePath != null).ToListAsync())
                                    .Where(m => NormalizeKey(m.FilePath) == fileKey)
                                    .ToList();

                                // Find keep entity (prefer one already having chosen title)
                                var keep = targetEntities.FirstOrDefault(t => string.Equals((t.Title ?? "").Trim(), chosenTitle.Trim(), StringComparison.OrdinalIgnoreCase))
                                    ?? targetEntities.FirstOrDefault();

                                if (keep != null)
                                {
                                    // Update its title to chosen
                                    keep.Title = chosenTitle;

                                    // Remove other target entities for this file
                                    foreach (var other in targetEntities.Where(e => e.ID != keep.ID))
                                    {
                                        _targetDb.Melody.Remove(other);
                                    }

                                    await _targetDb.SaveChangesAsync();
                                    _logger.LogInformation("Resolved title conflict for {File} -> kept title '{Title}'", fileKey, chosenTitle);
                                }

                                break;
                            }
                        }
                        Console.Write("Invalid choice. Enter number: ");
                    }
                }

                continue; // move to next file
            }

            // Case B: Titles same (or only one non-empty title), check physical file sizes
            var singleTitle = allTitles.FirstOrDefault() ?? string.Empty;

            var srcFirst = srcList.FirstOrDefault();
            var trgFirst = trgList.FirstOrDefault();
            if (srcFirst == null || trgFirst == null) continue;

            var srcPhysical = Path.Combine(GetSourceMelodiesPath(), fileKey);
            var trgPhysical = Path.Combine(melodiesPath, fileKey);

            if (File.Exists(srcPhysical) && File.Exists(trgPhysical))
            {
                var srcInfo = new FileInfo(srcPhysical);
                var trgInfo = new FileInfo(trgPhysical);
                if (srcInfo.Length != trgInfo.Length)
                {
                    if (!_interactiveMode)
                    {
                        // choose newer
                        if (srcInfo.LastWriteTime > trgInfo.LastWriteTime)
                        {
                            File.Copy(srcPhysical, trgPhysical, overwrite: true);
                            _logger.LogInformation("Auto-copied newer source MIDI for {File}", fileKey);
                        }
                        else
                        {
                            _logger.LogInformation("Kept target MIDI for {File} (newer).", fileKey);
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("=== MIDI SIZE CONFLICT ===");
                        Console.ResetColor();
                        Console.WriteLine($"File: {fileKey}");
                        Console.WriteLine($"Source size: {FormatFileSize(srcInfo.Length)} LastWrite: {srcInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine($"Target size: {FormatFileSize(trgInfo.Length)} LastWrite: {trgInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");
                        Console.WriteLine();
                        Console.WriteLine("Choose file to keep in target:");
                        Console.WriteLine("1 - Use source file");
                        Console.WriteLine("2 - Keep target file");
                        Console.WriteLine("0 - Skip");
                        Console.Write("Your choice (1/2/0): ");

                        while (true)
                        {
                            var input = Console.ReadLine()?.Trim();
                            if (input == "1")
                            {
                                File.Copy(srcPhysical, trgPhysical, overwrite: true);
                                _logger.LogInformation("Copied source MIDI over target for {File}", fileKey);
                                break;
                            }
                            if (input == "2")
                            {
                                _logger.LogInformation("Kept target MIDI for {File} (user choice)", fileKey);
                                break;
                            }
                            if (input == "0")
                            {
                                _logger.LogInformation("Skipped MIDI size conflict for {File}", fileKey);
                                break;
                            }
                            Console.Write("Invalid choice. Enter 1, 2 or 0: ");
                        }
                    }
                }
            }
        }

        _logger.LogInformation("Melodies sync complete.");

        List<Melody> GetUniqueSourceMelodies(List<Melody> sourceMelodies, Dictionary<string, List<Melody>> trgByFile, HashSet<string> targetTitlesSet)
        {
            return sourceMelodies.Where(src =>
            {
                var normTitle = NormalizeTitle(src.Title);
                var normFile = string.IsNullOrWhiteSpace(src.FilePath) ? null : NormalizeKey(src.FilePath);

                bool sameTitleInTarget = targetTitlesSet.Contains(normTitle);
                bool sameFileInTarget = !string.IsNullOrEmpty(normFile) && trgByFile.ContainsKey(normFile);

                return !sameTitleInTarget && !sameFileInTarget;
            }).ToList();
        }

        async Task PrepareMelodiesForCopying(string melodiesPath, List<Melody> uniqueSourceMelodies)
        {
            foreach (var src in uniqueSourceMelodies)
            {
                try
                {
                    if (src.Author == null) continue; // skip if no author information

                    var author = await EnsureAuthorInTargetAsync(src.Author);

                    // Add melody record to target
                    var newMelody = new Melody
                    {
                        Title = src.Title,
                        Author = author,
                        Year = src.Year,
                        Description = src.Description,
                        FilePath = src.FilePath,
                        IsFileEligible = src.IsFileEligible,
                        Tonality = src.Tonality,
                        AuthorID = author.ID
                    };
                    _targetDb.Melody.Add(newMelody);

                    // Copy physical MIDI file if exists in source
                    if (!string.IsNullOrWhiteSpace(src.FilePath))
                    {
                        var rel = NormalizeKey(src.FilePath);
                        var srcFile = Path.Combine(GetSourceMelodiesPath(), rel);
                        var dstFile = Path.Combine(melodiesPath, rel);

                        try
                        {
                            var dstDir = Path.GetDirectoryName(dstFile) ?? melodiesPath;
                            if (!Directory.Exists(dstDir)) Directory.CreateDirectory(dstDir);

                            if (File.Exists(srcFile))
                            {
                                File.Copy(srcFile, dstFile, overwrite: true);
                                _logger.LogInformation("Copied MIDI file for unique melody: {File}", rel);
                            }
                            else
                            {
                                _logger.LogWarning("Source MIDI file not found for unique melody: {File}", rel);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Failed to copy MIDI file {Src} -> {Dst}", srcFile, dstFile);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to add unique source melody '{Title}'", src.Title);
                }
            }
        }
    }

    private bool AskIfAddUniqueMelodies(List<Melody> uniqueSourceMelodies)
    {
        bool shouldAddUnique = true;
        if (uniqueSourceMelodies.Count > 0 && _interactiveMode)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("The following unique melodies were found in source and are planned to be added to the target:");
            Console.ResetColor();

            for (int i = 0; i < uniqueSourceMelodies.Count; i++)
            {
                var u = uniqueSourceMelodies[i];
                var author = u.Author != null ? $"{u.Author.Surname} {u.Author.Name}" : "<no author>";
                Console.WriteLine($"[{i + 1}] Title: '{u.Title}' | Author: {author} | File: {u.FilePath}");
            }

            Console.WriteLine();
            Console.Write("Add these melodies to target database? (y/n): ");
            while (true)
            {
                var ans = (Console.ReadLine() ?? string.Empty).Trim().ToLowerInvariant();
                if (ans == "y" || ans == "yes")
                {
                    shouldAddUnique = true;
                    break;
                }
                if (ans == "n" || ans == "no")
                {
                    shouldAddUnique = false;
                    break;
                }
                Console.Write("Please answer 'y' or 'n': ");
            }
        }
        else if (uniqueSourceMelodies.Count > 0 && !_interactiveMode)
        {
            _logger.LogInformation("Non-interactive mode: automatically adding {Count} unique melodies.", uniqueSourceMelodies.Count);
        }

        return shouldAddUnique;
    }

    // NEW: Синхронізація MIDI файлів
    private async Task SyncMidiFilesAsync()
    {
        _logger.LogInformation("Starting MIDI files synchronization...");

        var melodiesPath = Path.Combine(_environment.WebRootPath, "melodies");

        // Переконатися, що папка melodies існує в обох проєктах
        if (!Directory.Exists(melodiesPath))
        {
            Directory.CreateDirectory(melodiesPath);
            _logger.LogInformation("Created melodies directory: {Path}", melodiesPath);
        }

        // Отримати всіх мелодії з обох баз даних
        var sourceMelodies = await _sourceDb.Melody
            .Where(m => !string.IsNullOrEmpty(m.FilePath) && m.FilePath.EndsWith(".mid"))
            .Select(m => new { m.ID, m.FilePath, m.Title, AuthorSurname = m.Author.Surname })
            .ToListAsync();

        var targetMelodies = await _targetDb.Melody
            .Where(m => !string.IsNullOrEmpty(m.FilePath) && m.FilePath.EndsWith(".mid"))
            .Select(m => new { m.ID, m.FilePath, m.Title, AuthorSurname = m.Author.Surname })
            .ToListAsync();

        _logger.LogInformation("Found {SourceCount} source MIDI files and {TargetCount} target MIDI files",
            sourceMelodies.Count, targetMelodies.Count);

        // Нормалізація ключа шляху
        string NormalizeKey(string? p) => (p ?? "").Replace('\\', '/').TrimStart('/').ToLowerInvariant();

        // Групувати та виявляти дублікати у джерелі
        var srcGroups = sourceMelodies.GroupBy(m => NormalizeKey(m.FilePath)).ToList();
        var duplicatedSourceKeys = srcGroups.Where(g => g.Count() > 1).ToList();
        foreach (var g in duplicatedSourceKeys)
        {
            var sampleList = g.Select(d => $"{d.Title ?? "<no title>"} ({d.AuthorSurname})").Distinct().Take(5);
            var msg = $"File='{g.Key}' Count={g.Count()} Entries={string.Join("; ", sampleList)}";
            //_logger.LogWarning(msg);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Write("Duplicate source MIDI detected: ");
            Console.ResetColor();
            _logger.LogWarning(msg);
        }

        // Для подальшої обробки беремо перший елемент з кожної групи
        var sourceFileMap = srcGroups
            .Select(g => g.First())
            .ToDictionary(m => NormalizeKey(m.FilePath), m => m);

        // Групувати та виявляти дублікати у цілі
        var trgGroups = targetMelodies.GroupBy(m => NormalizeKey(m.FilePath)).ToList();
        var duplicatedTargetKeys = trgGroups.Where(g => g.Count() > 1).ToList();
        foreach (var g in duplicatedTargetKeys)
        {
            var sampleList = g.Select(d => $"{d.Title ?? "<no title>"} ({d.AuthorSurname})").Distinct().Take(5);
            var msg = $"Duplicate target MIDI detected: File='{g.Key}' Count={g.Count()} Entries={string.Join("; ", sampleList)}";
            _logger.LogWarning(msg);
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(msg);
            Console.ResetColor();
        }

        var targetFileMap = trgGroups
            .Select(g => g.First())
            .ToDictionary(m => NormalizeKey(m.FilePath), m => m);

        // Отримати всі унікальні імена файлів
        var allFileNames = sourceFileMap.Keys.Union(targetFileMap.Keys).ToHashSet();

        int copied = 0, skipped = 0, conflicts = 0;

        foreach (var fileName in allFileNames)
        {
            var sourceFilePath = Path.Combine(GetSourceMelodiesPath(), fileName);
            var targetFilePath = Path.Combine(melodiesPath, fileName);

            bool sourceExists = File.Exists(sourceFilePath);
            bool targetExists = File.Exists(targetFilePath);

            if (sourceExists && targetExists)
            {
                // Обидва файли існують - перевірити розміри
                var sourceInfo = new FileInfo(sourceFilePath);
                var targetInfo = new FileInfo(targetFilePath);

                if (sourceInfo.Length == targetInfo.Length)
                {
                    // Однаковий розмір - пропустити
                    skipped++;
                    _logger.LogDebug("File {FileName} has same size, skipping", fileName);
                }
                else
                {
                    // Різний розмір - запитати користувача
                    conflicts++;
                    var decision = await HandleFileConflictAsync(fileName, sourceInfo, targetInfo);

                    if (decision == FileDecision.UseSource)
                    {
                        File.Copy(sourceFilePath, targetFilePath, overwrite: true);
                        copied++;
                        _logger.LogInformation("Copied {FileName} from source (user choice)", fileName);
                    }
                    else if (decision == FileDecision.UseTarget)
                    {
                        // Нічого не робити, залишити цільовий файл
                        _logger.LogInformation("Kept target {FileName} (user choice)", fileName);
                    }
                }
            }
            else if (sourceExists && !targetExists)
            {
                // Тільки в джерелі - копіювати
                File.Copy(sourceFilePath, targetFilePath);
                copied++;
                _logger.LogInformation("Copied {FileName} from source", fileName);
            }
            else if (!sourceExists && targetExists)
            {
                // Тільки в цілі - залишити як є
                _logger.LogDebug("File {FileName} exists only in target, keeping", fileName);
            }
            else
            {
                // Файл в базі є, але фізично відсутній в обох місцях
                AddCollision($"MIDI file '{fileName}' referenced in database but missing from both directories");
            }
        }

        _logger.LogInformation("MIDI files sync complete. Copied: {Copied}, Skipped: {Skipped}, Conflicts: {Conflicts}",
copied, skipped, conflicts);
    }

    // NEW: Отримати шлях до папки melodies джерельного проєкту
    private string GetSourceMelodiesPath()
    {
        // Припускаємо, що джерельний проєкт знаходиться поруч з поточним
        var currentPath = _environment.WebRootPath;
        var currentProjectDir = Directory.GetParent(currentPath)?.FullName;
        var sourceProjectDir = Path.Combine(Directory.GetParent(currentProjectDir)!.FullName, "SourceProject", "wwwroot", "melodies");

        // Якщо стандартний шлях не існує, спробувати знайти в поточному проєкті
        if (!Directory.Exists(sourceProjectDir))
        {
            sourceProjectDir = Path.Combine(_environment.WebRootPath, "melodies_source");
        }

        // Якщо і цього немає, використати поточну папку melodies як джерело
        if (!Directory.Exists(sourceProjectDir))
        {
            sourceProjectDir = Path.Combine(_environment.WebRootPath, "melodies");
        }

        return sourceProjectDir;
    }

    // NEW: Отримати шлях до папки зображень джерела
    private string GetSourceImagesPath()
    {
        var currentPath = _environment.WebRootPath;
        var currentProjectDir = Directory.GetParent(currentPath)?.FullName;
        var sourceImagesDir = Path.Combine(Directory.GetParent(currentProjectDir)!.FullName, "SourceProject", "wwwroot", "images");

        if (!Directory.Exists(sourceImagesDir))
        {
            sourceImagesDir = Path.Combine(_environment.WebRootPath, "images_source");
        }

        if (!Directory.Exists(sourceImagesDir))
        {
            sourceImagesDir = Path.Combine(_environment.WebRootPath, "images");
        }

        return sourceImagesDir;
    }

    // NEW: Отримати шлях до папки зображень цілі
    private string GetTargetImagesPath()
    {
        var images = Path.Combine(_environment.WebRootPath, "images");
        if (!Directory.Exists(images))
            Directory.CreateDirectory(images);
        return images;
    }

    // NEW: Спробувати скопіювати зображення, якщо воно локальне (не HTTP URL)
    private async Task<bool> TryCopyImageForEntryAsync(string? imagePathOrUrl, string subfolder)
    {
        if (string.IsNullOrWhiteSpace(imagePathOrUrl)) return false;
        if (IsHttpUrl(imagePathOrUrl)) return false; // зовнішні URL не копіюємо тут

        try
        {
            // Якщо рядок виглядає як file:// URI, вилучимо префікс
            if (imagePathOrUrl.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                imagePathOrUrl = new Uri(imagePathOrUrl).LocalPath;
            }

            // Якщо це абсолютний шлях у файловій системі — використовуємо його напряму
            if (Path.IsPathRooted(imagePathOrUrl))
            {
                var absPath = imagePathOrUrl.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
                if (File.Exists(absPath))
                {
                    var targetRootAbs = Path.Combine(GetTargetImagesPath(), subfolder);
                    if (!Directory.Exists(targetRootAbs)) Directory.CreateDirectory(targetRootAbs);
                    var fileNameAbs = Path.GetFileName(absPath);
                    var targetFileAbs = Path.Combine(targetRootAbs, fileNameAbs);
                    File.Copy(absPath, targetFileAbs, overwrite: true);
                    _logger.LogInformation("Copied image {FileName} to target {TargetFile}", fileNameAbs, targetFileAbs);
                    return true;
                }

                return false;
            }

            // нормалізуємо відносний шлях (без ведучого '/')
            var relative = imagePathOrUrl.Replace('\\', '/').TrimStart('/');

            // Якщо шлях містить "wwwroot/" або провідний "images/" — прибираємо їх, щоб уникнути дублювання
            if (relative.StartsWith("wwwroot/", StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring("wwwroot/".Length);

            if (relative.StartsWith("images/", StringComparison.OrdinalIgnoreCase))
                relative = relative.Substring("images/".Length);

            // зберігати підкаталог (наприклад 'authors' або 'flags') для unikнення колізій
            var sourceRoot = GetSourceImagesPath();
            var targetRoot = Path.Combine(GetTargetImagesPath(), subfolder);
            if (!Directory.Exists(targetRoot)) Directory.CreateDirectory(targetRoot);

            // якщо в imagePathOrUrl є шляхи з підкаталогами - збережемо їх (take filename)
            var fileName = Path.GetFileName(relative);
            if (string.IsNullOrEmpty(fileName)) return false;

            // Спробуємо кілька варіантів пошуку у джерелі:
            //1) sourceRoot + relative
            //2) sourceRoot + fileName
            //3) якщо relative містить підпапки, ще раз перевірити без них
            var sourceFile = Path.Combine(sourceRoot, relative);
            if (!File.Exists(sourceFile))
            {
                sourceFile = Path.Combine(sourceRoot, fileName);
            }

            if (!File.Exists(sourceFile))
            {
                // Іноді у базі збережено шлях з провідним підкаталогом (наприклад "authors/...") — перевіримо так само
                var alt = relative.Contains('/') ? Path.GetFileName(relative) : relative;
                sourceFile = Path.Combine(sourceRoot, alt);
            }

            if (!File.Exists(sourceFile))
            {
                _logger.LogDebug("Source image not found: {SourceFile}", sourceFile);
                return false;
            }

            var targetFile = Path.Combine(targetRoot, fileName);

            // Якщо цільовий файл вже існує — оновлюємо тільки якщо джерело новіше
            if (File.Exists(targetFile))
            {
                var srcInfo = new FileInfo(sourceFile);
                var trgInfo = new FileInfo(targetFile);
                if (srcInfo.Length == trgInfo.Length && srcInfo.LastWriteTime <= trgInfo.LastWriteTime)
                {
                    _logger.LogDebug("Target image exists and is up-to-date: {TargetFile}", targetFile);
                    return true;
                }
            }

            // Копіюємо файл (синхронно — небагато файлів)
            File.Copy(sourceFile, targetFile, overwrite: true);
            _logger.LogInformation("Copied image {FileName} to target {TargetFile}", fileName, targetFile);

            // У випадку, якщо хочемо зберігати шлях у базі як відносний до wwwroot/images, можна повернути оновлений шлях.
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to copy image {Image} : {Message}", imagePathOrUrl, ex.Message);
            return false;
        }
    }

    // NEW: Проходити по авторам у цільовій базі і копіює фото тільки якщо поле Photo пусте
    private async Task SyncMissingAuthorPhotosAsync()
    {
        _logger.LogInformation("Starting sync of missing author photos (target-only check)...");

        if (_sourceDb == null || _targetDb == null)
        {
            _logger.LogWarning("Source or target DB context is null - skipping photo sync.");
            return;
        }

        List<Author> sourceAuthors;
        List<Author> targetAuthors;

        try
        {
            // Завантажити всіх авторів джерела (для пошуку відповідностей в пам'яті)
            sourceAuthors = await _sourceDb.Author.AsNoTracking().ToListAsync();

            // Завантажити всіх авторів цілі
            targetAuthors = await _targetDb.Author.ToListAsync(); // will update some records
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load authors for photo synchronization. Aborting photo sync.");
            return;
        }

        int copied = 0, assigned = 0, skipped = 0, notFound = 0;

        _logger.LogInformation("Starting foreach...");

        try
        {
            foreach (var target in targetAuthors)
            {
                _logger.LogDebug("Processing target author ID {AuthorID} '{Surname} {Name}'", target.ID, target.Surname, target.Name);

                // Якщо в цільовому записі вже є Photo — пропускаємо
                if (!string.IsNullOrWhiteSpace(target.Photo))
                {
                    _logger.LogInformation("Author ID {AuthorID} '{Surname} {Name}' already has photo = {Photo}, skipping", target.ID, target.Surname, target.Name, target.Photo);
                    continue;
                }

                // Знайти відповідника у джерелі
                Author? src = GetAuthorInSource(sourceAuthors, target);

                // Якщо не знайдено джерело або в джерелі немає Photo — пропускаємо
                if (src == null || string.IsNullOrWhiteSpace(src.Photo))
                {
                    notFound++;
                    _logger.LogDebug("No source author or photo found for target author ID {AuthorID} '{Surname} {Name}'", target.ID, target.Surname, target.Name);
                    continue;
                }

                // Якщо це зовнішній URL — просто скопіювати посилання у цільову базу
                if (IsHttpUrl(src.Photo))
                {
                    target.Photo = src.Photo;
                    assigned++;
                    _logger.LogDebug("Assigned external URL photo for author ID {AuthorID} '{Surname} {Name}'", target.ID, target.Surname, target.Name);
                    continue;
                }

                // Локальний або відносний шлях - визначити ім'я файлу
                var fileName = Path.GetFileName(src.Photo.Replace('\\', '/'));
                if (string.IsNullOrEmpty(fileName))
                {
                    // Немає валідного імені файлу — все одно зберігаємо те, що є
                    target.Photo = src.Photo;
                    assigned++;
                    skipped++;
                    continue;
                }

                // Намір зберегти в "images/authors/<fileName>" у цільовому проекті
                var relativeTargetPath = Path.Combine("images", "authors", fileName).Replace('\\', '/');

                // Спробувати скопіювати файл (необхідно, але якщо не вдасться — все одно зберігаємо шлях)
                var copiedOk = false;
                try
                {
                    copiedOk = await TryCopyImageForEntryAsync(src.Photo, "authors");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Exception while trying to copy image for source {SourcePhoto}", src.Photo);
                    copiedOk = false;
                }

                if (copiedOk)
                {
                    copied++;
                }
                else
                {
                    skipped++;
                    _logger.LogDebug("Image copy failed or source not found for {SourcePhoto}; still assigning path {TargetPath}", src.Photo, relativeTargetPath);
                }

                // Призначити відносний шлях у базі незалежно від результату копіювання
                target.Photo = relativeTargetPath;
                assigned++;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during photo synchronization loop.");
        }

        if (assigned > 0)
        {
            try
            {
                await _targetDb.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save photo assignments to target database.");
            }
        }

        _logger.LogInformation("Missing author photos sync complete. Assigned: {Assigned}, Copied: {Copied}, Skipped: {Skipped}, NotFound: {NotFound}", assigned, copied, skipped, notFound);
    }

    private static Author? GetAuthorInSource(List<Author> sourceAuthors, Author target)
    {
        Author? src = sourceAuthors.FirstOrDefault(sa => Same(sa.Name, target.Name) && Same(sa.Surname, target.Surname));

        if (src == null && !string.IsNullOrWhiteSpace(target.NameEn) && !string.IsNullOrWhiteSpace(target.SurnameEn))
        {
            src = sourceAuthors.FirstOrDefault(sa => Same(sa.NameEn, target.NameEn) && Same(sa.SurnameEn, target.SurnameEn));
        }

        if (src == null)
        {
            src = sourceAuthors.FirstOrDefault(sa => Same(sa.Surname, target.Surname));
        }

        return src;
    }

    // NEW: Прості утиліти
    private static bool IsHttpUrl(string s)
    {
        if (string.IsNullOrWhiteSpace(s)) return false;
        return s.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || s.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    // NEW: Enum для рішень щодо файлів
    public enum FileDecision
    {
        UseSource,
        UseTarget,
        Skip
    }

    // NEW: Обробка конфліктів файлів
    private async Task<FileDecision> HandleFileConflictAsync(string fileName, FileInfo sourceInfo, FileInfo targetInfo)
    {
        if (!_interactiveMode)
        {
            // В автоматичному режимі - використати новіший файл
            if (sourceInfo.LastWriteTime > targetInfo.LastWriteTime)
            {
                _logger.LogInformation("Auto-selecting source file {FileName} (newer: {SourceDate} > {TargetDate})",
           fileName, sourceInfo.LastWriteTime, targetInfo.LastWriteTime);
                return FileDecision.UseSource;
            }
            else
            {
                _logger.LogInformation("Auto-keeping target file {FileName} (newer or same: {TargetDate} >= {SourceDate})",
     fileName, targetInfo.LastWriteTime, sourceInfo.LastWriteTime);
                return FileDecision.UseTarget;
            }
        }

        // Інтерактивний режим
        Console.WriteLine();
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("=== КОНФЛІКТ MIDI ФАЙЛУ ===");
        Console.ResetColor();

        Console.WriteLine($"Файл: {fileName}");
        Console.WriteLine();

        Console.WriteLine("ДЖЕРЕЛЬНИЙ ФАЙЛ:");
        Console.WriteLine($"  Розмір: {FormatFileSize(sourceInfo.Length)}");
        Console.WriteLine($"  Дата зміни: {sourceInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        Console.WriteLine();
        Console.WriteLine("ЦІЛЬОВИЙ ФАЙЛ:");
        Console.WriteLine($"  Розмір: {FormatFileSize(targetInfo.Length)}");
        Console.WriteLine($"  Дата зміни: {targetInfo.LastWriteTime:yyyy-MM-dd HH:mm:ss}");

        Console.WriteLine();
        Console.WriteLine("Оберіть файл:");
        Console.WriteLine("1 - Використати джерельний файл");
        Console.WriteLine("2 - Залишити цільовий файл");
        Console.WriteLine("0 - Пропустити цей файл");
        Console.Write("Ваш вибір (1/2/0): ");

        while (true)
        {
            var input = Console.ReadLine()?.Trim();
            switch (input)
            {
                case "1":
                    Console.WriteLine("✓ Буде використано джерельний файл.");
                    return FileDecision.UseSource;
                case "2":
                    Console.WriteLine("✓ Цільовий файл залишиться без змін.");
                    return FileDecision.UseTarget;
                case "0":
                    Console.WriteLine("✓ Файл пропущено.");
                    return FileDecision.Skip;
                default:
                    Console.Write("Некоректний вибір. Введіть 1, 2 або 0: ");
                    break;
            }
        }
    }

    // NEW: Форматування розміру файлу
    private static string FormatFileSize(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB" };
        int counter = 0;
        decimal number = bytes;

        while (Math.Round(number / 1024) >= 1)
        {
            number /= 1024;
            counter++;
        }

        return $"{number:n1} {suffixes[counter]}";
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
            Console.WriteLine("• Точний збіг імені та прізвище");
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
                Console.WriteLine($"  [{i + 1}] {existing.Surname} {existing.Name} | {existing.SurnameEn} {existing.NameEn} (ID: {existing.ID})");
                Console.WriteLine($"      Країна: {existing.Country?.Name} | Роки: {existing.DateOfBirth}-{existing.DateOfDeath}");
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

        // Об'єднання опису англійською
        if (string.IsNullOrWhiteSpace(targetAuthor.DescriptionEn) && !string.IsNullOrWhiteSpace(sourceAuthor.DescriptionEn))
        {
            targetAuthor.DescriptionEn = sourceAuthor.DescriptionEn;
            hasChanges = true;
        }
        else if (!string.IsNullOrWhiteSpace(targetAuthor.DescriptionEn) && !string.IsNullOrWhiteSpace(sourceAuthor.DescriptionEn))
        {
            if (!targetAuthor.DescriptionEn.Contains(sourceAuthor.DescriptionEn))
            {
                targetAuthor.DescriptionEn += "\n\n--- From source DB ---\n" + sourceAuthor.DescriptionEn;
                hasChanges = true;
            }
        }

        // Об'єднання фото
        if (string.IsNullOrWhiteSpace(targetAuthor.Photo) && !string.IsNullOrWhiteSpace(sourceAuthor.Photo))
        {
            targetAuthor.Photo = sourceAuthor.Photo;
            hasChanges = true;
        }

        if (hasChanges)
        {
            await _targetDb.SaveChangesAsync();
            _logger.LogInformation($"Merged author '{sourceAuthor.Surname} {sourceAuthor.Name}' with existing author ID {targetAuthor.ID}.");
        }

        // Скопіювати файл фото, якщо потрібно
        if (!string.IsNullOrWhiteSpace(sourceAuthor.Photo))
        {
            await TryCopyImageForEntryAsync(sourceAuthor.Photo, "authors");
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

    // NEW: Синхронізація англійських імен авторів
    private async Task SyncAuthorEnglishNamesAsync()
    {
        _logger.LogInformation("Starting English names synchronization for authors...");

        // Отримати всіх авторів з обох баз даних
        var sourceAuthors = await _sourceDb.Author.AsNoTracking().ToListAsync();
        var targetAuthors = await _targetDb.Author.ToListAsync(); // без AsNoTracking для можливості оновлення

        int updatedCount = 0;
        int enrichedCount = 0;

        foreach (var sourceAuthor in sourceAuthors)
        {
            // Знайти відповідного автора в цільовій базі
            var targetAuthor = targetAuthors.FirstOrDefault(ta =>
                Same(ta.Name, sourceAuthor.Name) &&
                Same(ta.Surname, sourceAuthor.Surname));

            if (targetAuthor == null)
            {
                // Спробувати знайти по англійських іменах
                targetAuthor = targetAuthors.FirstOrDefault(ta =>
                    !string.IsNullOrWhiteSpace(sourceAuthor.NameEn) &&
                    !string.IsNullOrWhiteSpace(sourceAuthor.SurnameEn) &&
                    Same(ta.NameEn, sourceAuthor.NameEn) &&
                    Same(ta.SurnameEn, sourceAuthor.SurnameEn));
            }

            if (targetAuthor != null)
            {
                bool hasChanges = false;

                // Оновити англійські імена якщо вони відсутні в цільовій базі
                if (string.IsNullOrWhiteSpace(targetAuthor.NameEn) &&
                    !string.IsNullOrWhiteSpace(sourceAuthor.NameEn))
                {
                    targetAuthor.NameEn = sourceAuthor.NameEn;
                    hasChanges = true;
                }

                if (string.IsNullOrWhiteSpace(targetAuthor.SurnameEn) &&
                    !string.IsNullOrWhiteSpace(sourceAuthor.SurnameEn))
                {
                    targetAuthor.SurnameEn = sourceAuthor.SurnameEn;
                    hasChanges = true;
                }

                // Оновити українські імена якщо вони відсутні в цільовій базі (зворотний напрямок)
                if (string.IsNullOrWhiteSpace(targetAuthor.Name) &&
                    !string.IsNullOrWhiteSpace(sourceAuthor.Name))
                {
                    targetAuthor.Name = sourceAuthor.Name;
                    hasChanges = true;
                }

                if (string.IsNullOrWhiteSpace(targetAuthor.Surname) &&
                    !string.IsNullOrWhiteSpace(sourceAuthor.Surname))
                {
                    targetAuthor.Surname = sourceAuthor.Surname;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    updatedCount++;
                    _logger.LogDebug("Updated author names: {Surname} {Name} -> {SurnameEn} {NameEn}",
                        targetAuthor.Surname, targetAuthor.Name, targetAuthor.SurnameEn, targetAuthor.NameEn);
                }
            }
        }

        // Збереження змін у цільовій базі
        if (updatedCount > 0)
        {
            await _targetDb.SaveChangesAsync();
            _logger.LogInformation("Updated English names for {Count} authors in target database", updatedCount);
        }

        // Тепер синхронізуємо зворотний напрямок: з цільової бази в джерельну
        var sourceAuthorsForUpdate = await _sourceDb.Author.ToListAsync();

        foreach (var targetAuthor in targetAuthors)
        {
            var sourceAuthor = sourceAuthorsForUpdate.FirstOrDefault(sa =>
                Same(sa.Name, targetAuthor.Name) &&
                Same(sa.Surname, targetAuthor.Surname));

            if (sourceAuthor == null)
            {
                // Спробувати знайти по англійських іменах
                sourceAuthor = sourceAuthorsForUpdate.FirstOrDefault(sa =>
                    !string.IsNullOrWhiteSpace(targetAuthor.NameEn) &&
                    !string.IsNullOrWhiteSpace(targetAuthor.SurnameEn) &&
                    Same(sa.NameEn, targetAuthor.NameEn) &&
                    Same(sa.SurnameEn, targetAuthor.SurnameEn));
            }

            if (sourceAuthor != null)
            {
                bool hasChanges = false;

                // Оновити англійські імена в джерельній базі
                if (string.IsNullOrWhiteSpace(sourceAuthor.NameEn) &&
                    !string.IsNullOrWhiteSpace(targetAuthor.NameEn))
                {
                    sourceAuthor.NameEn = targetAuthor.NameEn;
                    hasChanges = true;
                }

                if (string.IsNullOrWhiteSpace(sourceAuthor.SurnameEn) &&
                    !string.IsNullOrWhiteSpace(targetAuthor.SurnameEn))
                {
                    sourceAuthor.SurnameEn = targetAuthor.SurnameEn;
                    hasChanges = true;
                }

                if (hasChanges)
                {
                    enrichedCount++;
                }
            }
        }

        // Збереження змін у джерельній базі
        if (enrichedCount > 0)
        {
            await _sourceDb.SaveChangesAsync();
            _logger.LogInformation("Enriched English names for {Count} authors in source database", enrichedCount);
        }

        // Генерування англійських імен через транслітерацію для авторів без них
        await GenerateTransliteratedNamesAsync();

        _logger.LogInformation("English names sync complete. Updated: {Updated}, Enriched: {Enriched}",
            updatedCount, enrichedCount);
    }

    // NEW: Генерування англійських імен через транслітерацію
    private async Task GenerateTransliteratedNamesAsync()
    {
        _logger.LogInformation("Generating transliterated English names for authors without them...");

        int generatedInSource = 0;
        int generatedInTarget = 0;

        // Обробка джерельної бази
        var sourceAuthorsWithoutEn = await _sourceDb.Author
            .Where(a => (string.IsNullOrWhiteSpace(a.NameEn) || string.IsNullOrWhiteSpace(a.SurnameEn)) &&
                        (!string.IsNullOrWhiteSpace(a.Name) || !string.IsNullOrWhiteSpace(a.Surname)))
            .ToListAsync();

        foreach (var author in sourceAuthorsWithoutEn)
        {
            bool hasChanges = false;

            if (string.IsNullOrWhiteSpace(author.NameEn) && !string.IsNullOrWhiteSpace(author.Name))
            {
                author.NameEn = Translit.Transliterate(author.Name);
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(author.SurnameEn) && !string.IsNullOrWhiteSpace(author.Surname))
            {
                // Особлива обробка для "Українська народна пісня"
                if (author.Surname.Equals("Українська народна пісня", StringComparison.OrdinalIgnoreCase))
                {
                    author.SurnameEn = "Ukrainian folk song";
                }
                else
                {
                    author.SurnameEn = Translit.Transliterate(author.Surname);
                }
                hasChanges = true;
            }

            if (hasChanges)
            {
                generatedInSource++;
                _logger.LogDebug("Generated English names for source author: {Surname} {Name} -> {SurnameEn} {NameEn}",
                    author.Surname, author.Name, author.SurnameEn, author.NameEn);
            }
        }

        if (generatedInSource > 0)
        {
            await _sourceDb.SaveChangesAsync();
        }

        // Обробка цільової бази
        var targetAuthorsWithoutEn = await _targetDb.Author
            .Where(a => (string.IsNullOrWhiteSpace(a.NameEn) || string.IsNullOrWhiteSpace(a.SurnameEn)) &&
                        (!string.IsNullOrWhiteSpace(a.Name) || !string.IsNullOrWhiteSpace(a.Surname)))
            .ToListAsync();

        foreach (var author in targetAuthorsWithoutEn)
        {
            bool hasChanges = false;

            if (string.IsNullOrWhiteSpace(author.NameEn) && !string.IsNullOrWhiteSpace(author.Name))
            {
                author.NameEn = Translit.Transliterate(author.Name);
                hasChanges = true;
            }

            if (string.IsNullOrWhiteSpace(author.SurnameEn) && !string.IsNullOrWhiteSpace(author.Surname))
            {
                if (author.Surname.Equals("Українська народна пісня", StringComparison.OrdinalIgnoreCase))
                {
                    author.SurnameEn = "Ukrainian folk song";
                }
                else
                {
                    author.SurnameEn = Translit.Transliterate(author.Surname);
                }
                hasChanges = true;
            }

            if (hasChanges)
            {
                generatedInTarget++;
                _logger.LogDebug("Generated English names for target author: {Surname} {Name} -> {SurnameEn} {NameEn}",
                    author.Surname, author.Name, author.SurnameEn, author.NameEn);
            }
        }

        if (generatedInTarget > 0)
        {
            await _targetDb.SaveChangesAsync();
        }

        _logger.LogInformation("Generated transliterated names. Source: {SourceCount}, Target: {TargetCount}",
            generatedInSource, generatedInTarget);
    }

    // Ensure author exists in target DB; create if missing, copy country/photo as needed
    private async Task<Author> EnsureAuthorInTargetAsync(Author srcAuthor)
    {
        if (srcAuthor == null) throw new ArgumentNullException(nameof(srcAuthor));

        var normSurname = (srcAuthor.Surname ?? string.Empty).Trim().ToLowerInvariant();
        var author = await _targetDb.Author.Include(a => a.Country)
        .FirstOrDefaultAsync(a => ((a.Surname ?? string.Empty).Trim().ToLower()) == normSurname);

        if (author != null)
            return author;

        Country? country = null;
        if (srcAuthor.Country != null)
        {
            var normCountry = (srcAuthor.Country.Name ?? string.Empty).Trim().ToLowerInvariant();
            country = await _targetDb.Country.FirstOrDefaultAsync(c => ((c.Name ?? string.Empty).Trim().ToLower()) == normCountry);
            if (country == null)
            {
                country = new Country
                {
                    Name = srcAuthor.Country.Name,
                    NameEn = srcAuthor.Country.NameEn,
                    FlagUrl = srcAuthor.Country.FlagUrl
                };
                _targetDb.Country.Add(country);
                await _targetDb.SaveChangesAsync();
            }
        }

        var newAuthor = new Author
        {
            Name = srcAuthor.Name,
            Surname = srcAuthor.Surname,
            NameEn = srcAuthor.NameEn,
            SurnameEn = srcAuthor.SurnameEn,
            Country = country,
            DateOfBirth = srcAuthor.DateOfBirth,
            DateOfDeath = srcAuthor.DateOfDeath,
            Description = srcAuthor.Description,
            DescriptionEn = srcAuthor.DescriptionEn,
            Photo = srcAuthor.Photo
        };

        _targetDb.Author.Add(newAuthor);
        await _targetDb.SaveChangesAsync();

        if (!string.IsNullOrWhiteSpace(srcAuthor.Photo))
        {
            try
            {
                await TryCopyImageForEntryAsync(srcAuthor.Photo, "authors");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to copy author photo for {Author}", srcAuthor.Surname);
            }
        }

        return newAuthor;
    }
}
