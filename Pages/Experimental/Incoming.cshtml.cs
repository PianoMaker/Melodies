using Melodies25.Models;
using Melodies25.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.IO;
using System.Text.Json;

namespace Melodies25.Pages.Experimental
{
    public class IncomingModel : PageModel
    {

        public List<(Melody, string, int, string)> IncomingMelodies = new List<(Melody, string, int, string)>();
        private readonly IWebHostEnvironment _environment;
        private const string IncomingWebFolder = "/melodies/incoming";
        private readonly Melodies25.Data.Melodies25Context _context;


        public IncomingModel(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public void OnGet()
        {
            Console.WriteLine("Incoming page accessed");

            // Simulate fetching incoming melodies
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

                    // Map DTO to display Melody (do not touch DB)
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
        public async Task OnPostSaveAsync(int id)
        {
            
            Console.WriteLine($"Save requested for incoming melody ID: {id}");
            var melodyToSave = IncomingMelodies.FirstOrDefault(item => item.Item1.ID == id);
            if (melodyToSave.Item1 != null)
            {
                _context.Melody.Add(melodyToSave.Item1);
                await _context.SaveChangesAsync();

                Console.WriteLine($"Saving melody: {melodyToSave.Item1.Title}");
            }

            DeleteFileFromWebDirectory(id);

            IncomingMelodies = GetIncomingMelodies();
            Page();
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

        

        private void DeleteFileFromWebDirectory(int id)
        {
            Directory.GetFiles(Path.Combine(_environment.WebRootPath ?? string.Empty, "melodies", "incoming"), "*.json")
                            .Where(jsonPath =>
                            {
                                try
                                {
                                    var json = System.IO.File.ReadAllText(jsonPath);
                                    var dto = JsonSerializer.Deserialize<IncomingDto>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                                    return dto != null && dto.Id == id;
                                }
                                catch
                                {
                                    return false;
                                }
                            })
                            .ToList()
                            .ForEach(jsonPath =>
                            {
                                try
                                {
                                    System.IO.File.Delete(jsonPath);
                                    Console.WriteLine($"Deleted incoming JSON file: {jsonPath}");
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"Failed to delete incoming JSON file '{jsonPath}': {ex.Message}");
                                }
                            });
        }


    }
}
