using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Text.Json;

namespace Melodies25.Pages.Experimental
{
    [Authorize(Roles = "Admin, Moderator")] // Доступ лише для адміністратора або модератора
    public class IncomingModel : PageModel
    {

        public List<(Melody, string, int, string)> IncomingMelodies = new List<(Melody, string, int, string)>();
        private readonly IWebHostEnvironment _environment;
        private const string IncomingWebFolder = "/melodies/incoming";
        private readonly Melodies25.Data.Melodies25Context _context;


        public IncomingModel(IWebHostEnvironment environment, Melodies25.Data.Melodies25Context context)
        {
            _environment = environment;
            _context = context;
        }

        public void OnGet()
        {
            Console.WriteLine("Incoming page accessed");
            IncomingMelodies = GetIncomingMelodies();
        }

        private List<(Melody, string, int, string)> GetIncomingMelodies()
        {
            var list = new List<(Melody, string, int, string)>();

            var incomingDir = Path.Combine(_environment.WebRootPath ?? string.Empty, "melodies", "incoming");
            if (!Directory.Exists(incomingDir))
            {
                return list;
            }

            // Read only JSON files written by Create -> SaveMelodyJsonAsync
            var jsonFiles = Directory.GetFiles(incomingDir, "*.json");
            int index = 0;

            foreach (var jsonPath in jsonFiles)
            {
                try
                {
                    var json = System.IO.File.ReadAllText(jsonPath);
                    // DTO matches the shape produced in SaveMelodyJsonAsync
                    var dto = JsonSerializer.Deserialize<IncomingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    if (dto is null) continue;

                    // Map DTO to display MusicMelody (do not touch DB)
                    var m = new Melody();

                    // fill the most useful fields for UI
                    m.ID = dto.Id;
                    m.Title = dto.Title;
                    // if JSON contains FilePath (filename), convert to web-relative path in incoming folder
                    if (!string.IsNullOrWhiteSpace(dto.FilePath))
                    {
                        var midiFileName = Path.GetFileName(dto.FilePath);
                        m.FilePath = $"{IncomingWebFolder}/{midiFileName}";
                    }
                    else
                    {
                        // fallback: try to infer corresponding midi by replacing .json with .mid
                        var inferredMidi = Path.GetFileNameWithoutExtension(jsonPath) + ".mid";
                        m.FilePath = $"{IncomingWebFolder}/{inferredMidi}";
                    }

                    m.Tonality = dto.Tonality;
                    // attach author info (display only; not an entity save)
                    if (dto.Author is not null)
                    {
                        m.Author = new Author { Name = dto.Author.Name ?? "", Surname = dto.Author.Surname ?? "" };
                    }

                    // Optionally include other fields in Description to show on UI
                    m.Description = dto.Description ?? "";

                    var fileinfo = $"AddedBy: {dto.AddedBy}";

                    var addedAt = dto.AddedAt ?? "";

                    list.Add((m, fileinfo, index++, addedAt));
                }
                catch (Exception ex)
                {
                    // log and continue
                    Console.WriteLine($"Failed to read/parse incoming JSON '{jsonPath}': {ex.Message}");
                    continue;
                }
            }

            return list;
        }

        // small DTOs matching SaveMelodyJsonAsync output
        private class IncomingDto
        {
            public int Id { get; set; }
            public string? Title { get; set; }
            public AuthorDto? Author { get; set; }
            public string? FilePath { get; set; }
            public string? Tonality { get; set; }
            public int Tempo { get; set; }

            public string? Description { get; set; }

            public string? AddedBy { get; set; }
            public string? AddedAt { get; set; }
        }

        private class AuthorDto
        {
            public string? Name { get; set; }
            public string? Surname { get; set; }
        }

        public void OnPost(int id)
        {
            // Placeholder for future POST handling
            Console.WriteLine($"onpost pressed id = {id}");
        }

        // Збереження мелодії до бази даних
        public async Task<IActionResult> OnPostSaveAsync(int id)
        {
            IncomingMelodies = GetIncomingMelodies();
            Console.WriteLine($"Save requested for incoming melody ID: {id}. Incoming melodies count = {IncomingMelodies.Count}");                       

            try
            {
                var melodyToSave = IncomingMelodies[id];

                var incomingMelody = melodyToSave.Item1;


                if (incomingMelody is null)
                {
                    Console.WriteLine("Incoming melody not found");
                    TempData["Message"] = "Мелодію не знайдено";
                    return Page();
                }


                bool exists = _context.Melody.Any(m => m.Title == incomingMelody.Title);
                if (exists)
                {
                    Console.WriteLine("MusicMelody already exists in DB");
                    TempData["Message"] = "Мелодія вже існує в базі";
                    DeleteFileFromWebDirectory(id); // опціонально
                    return RedirectToPage();
                }

                var entity = new Melody
                {
                    Title = incomingMelody.Title,
                    Description = incomingMelody.Description,
                    Tonality = incomingMelody.Tonality
                };

                if (incomingMelody.Author is not null && !string.IsNullOrWhiteSpace(incomingMelody.Author.Surname))
                {
                    var existingAuthor = _context.Author.FirstOrDefault(a => a.Surname == incomingMelody.Author.Surname);
                    if (existingAuthor is not null)
                    {
                        entity.AuthorID = existingAuthor.ID;
                    }
                    else
                    {
                        // або створити нового автора (обережно з дублями)
                        var newAuthor = new Author
                        {
                            Surname = incomingMelody.Author.Surname,
                            Name = incomingMelody.Author.Name
                        };
                        _context.Author.Add(newAuthor);
                        await _context.SaveChangesAsync();
                        entity.AuthorID = newAuthor.ID;
                    }
                }
                _context.Melody.Add(entity);
                
                await _context.SaveChangesAsync();

                // Фалй MIDI переміщуємо після збереження сутності
                MoveMidiFile(incomingMelody, entity);

                Console.WriteLine($"Saving melody: {entity.Title}");
                TempData["Message"] = $"Мелодію '{entity.Title}' збережено";

                DeleteFileFromWebDirectory(id);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to save melody: {ex.Message}");
                TempData["Message"] = $"Помилка збереження: {ex.Message}";
                return Page();
            }

            //DeleteFileFromWebDirectory(id);

            IncomingMelodies = GetIncomingMelodies();
            return Page();
        }

        private void MoveMidiFile(Melody incomingMelody, Melody entity)
        {
            try
            {
                var webRoot = _environment.WebRootPath ?? string.Empty;
                var incomingFolder = Path.Combine(webRoot, "melodies", "incoming");
                var targetFolder = Path.Combine(webRoot, "melodies");
                Directory.CreateDirectory(targetFolder);

                // Get the file name from the web-relative path we built when listing incoming files
                var fileName = Path.GetFileName(incomingMelody.FilePath ?? string.Empty);

                if (!string.IsNullOrWhiteSpace(fileName))
                {
                                       
                    var sourcePath = Path.Combine(incomingFolder, fileName);
                    if (!System.IO.File.Exists(sourcePath))
                    {
                        // Fallback: try force .mid
                        var baseName = Path.GetFileNameWithoutExtension(fileName);
                        var altSource = Path.Combine(incomingFolder, $"{baseName}.mid");
                        if (System.IO.File.Exists(altSource))
                        {
                            sourcePath = altSource;
                            fileName = $"{baseName}.mid";
                        }
                    }

                    if (System.IO.File.Exists(sourcePath))
                    {
                        var destPath = Path.Combine(targetFolder, fileName);

                        // If destination exists, make a unique file name
                        if (System.IO.File.Exists(destPath))
                        {
                            var uniqueFile = $"{Path.GetFileNameWithoutExtension(fileName)}_{DateTime.UtcNow:yyyyMMddHHmmss}{Path.GetExtension(fileName)}";
                            destPath = Path.Combine(targetFolder, uniqueFile);
                            fileName = uniqueFile;
                        }

                        System.IO.File.Move(sourcePath, destPath);

                        Console.WriteLine($"MIDI moved to: {destPath}");
                    }
                    else
                    {
                        Console.WriteLine($"MIDI source file not found. Expected at: {sourcePath}");
                    }
                }
                else
                {
                    Console.WriteLine("Incoming melody FilePath missing; cannot move MIDI.");
                }
            }
            catch (Exception moveEx)
            {
                Console.WriteLine($"Failed to move MIDI file: {moveEx.Message}");
                // Continue; melody is already saved. We keep JSON cleanup to avoid reprocessing.
            }
        }

        // Видалення вхідної мелодії
        public async Task OnPostDelete(int id)
        {
            // Placeholder for future POST handling
            Console.WriteLine($"Delete requested for incoming melody ID: {id}");
            IncomingMelodies.RemoveAll(item => item.Item1.ID == id);
            DeleteFileFromWebDirectory(id);


            IncomingMelodies = GetIncomingMelodies();
            Page();
        }

        public async Task<IActionResult> OnPostPreviewAsync(int id)
        {

            // Placeholder for future POST handling
            Console.WriteLine($"Preview requested for incoming melody ID: {id}");
            IncomingMelodies = GetIncomingMelodies();
            var melodyToPreview = IncomingMelodies[id];
            var incomingMelody = melodyToPreview.Item1;
            if (incomingMelody is null)
            {
                Console.WriteLine("Incoming melody not found for preview");
                TempData["Message"] = "Мелодію не знайдено для попереднього перегляду";
                return Page();
            }
            // Redirect to a preview page with the melody details
            return RedirectToPage("/Melodies/Preview", new { path = incomingMelody.FilePath, name = incomingMelody.Title });
        }


        private void DeleteFileFromWebDirectory(int id)
        {
            int counter = 0;    
            Console.WriteLine($"Deleting files for incoming melody ID: {id}");
            var allfiles = Directory.GetFiles(Path.Combine(_environment.WebRootPath ?? string.Empty, "melodies", "incoming"));
            var file = allfiles[id];

            Console.WriteLine($"Deleting file {file}");


            try
            {
                System.IO.File.Delete(file);
                counter++;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to delete file '{file}': {ex.Message}");
            }


            Console.WriteLine($"Deletion process completed for incoming melody ID: {id}, {counter} files deleted");
        }


    }
}
