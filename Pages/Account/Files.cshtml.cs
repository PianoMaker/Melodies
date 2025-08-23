using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Music;
using static Music.Messages;
using Melodies25.Data;
using Melodies25.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using System.Text;

namespace Melodies25.Pages.Account
{
    [BindProperties]
    public class FilesModel : PageModel
    {
        public List<FileInformation>? TempFiles { get; set; } = new();
        public List<FileInformation>? MidiFiles { get; set; } = new();
        public List<FileInformation>? Mp3Files { get; set; } = new();

        public int NumberOfFiles;
        public int TotalSize;

        public struct FileInformation
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public int Size { get; set; }
            public string Date { get; set; }
        }

        [BindProperty]
        public int Days { get; set; }

        private readonly IWebHostEnvironment _environment;
        private readonly Melodies25Context _workingContext; // working DB

        public FilesModel(IWebHostEnvironment environment, Melodies25Context workingContext)
        {
            _environment = environment;
            _workingContext = workingContext;
        }

        public void OnGet()
        {
            MessageL(COLORS.yellow, "FileNames/OnGet method starts");
            InitializeFiles();
        }

        private void InitializeFiles()
        {
            TempFiles = GetFiles("temporary");
            MidiFiles = GetFiles("melodies");
            Mp3Files = GetFiles("mp3");
            NumberOfFiles = TempFiles.Count + MidiFiles.Count + Mp3Files.Count;
            TotalSize = GetTotalSize();
        }

        private int GetTotalSize()
        {
            int tempFilesSize = TempFiles?.Sum(file => file.Size) ?? 0;
            int midiFilesSize = MidiFiles?.Sum(file => file.Size) ?? 0;
            int mp3FilesSize = Mp3Files?.Sum(file => file.Size) ?? 0;
            return (tempFilesSize + midiFilesSize + mp3FilesSize) / 1024;
        }

        private List<FileInformation> GetFiles(string subcategory)
        {
            var collection = new List<FileInformation>();
            var fileslist = Path.Combine(_environment.WebRootPath, subcategory);
            if (Directory.Exists(fileslist))
            {
                var FileNames = Directory.GetFiles(fileslist).Select(Path.GetFileName).ToList();
                int currentId = 0;
                foreach (var file in FileNames)
                {
                    if (file == null) continue;
                    var filePath = Path.Combine(fileslist, file);
                    try
                    {
                        var fileInfo = new FileInformation
                        {
                            Id = currentId++.ToString(),
                            Name = Path.GetFileName(filePath),
                            Path = file ?? "not_found",
                            Size = (int)new FileInfo(filePath).Length,
                            Date = System.IO.File.GetLastWriteTime(filePath).ToString()
                        };
                        collection.Add(fileInfo);
                    }
                    catch (Exception e)
                    {
                        ErrorMessageL($"could not process {file}: {e.Message}");
                        continue;
                    }
                }
            }
            else
            {
                MessageL(COLORS.red, "Directory not found");
            }
            return collection;
        }

        public void OnPostDeleteFile(int Id)
        {
            MessageL(COLORS.yellow, "FileNames/DeleteFile method starts");
            TempFiles.RemoveAt(Id);
            MessageL(COLORS.yellow, "file removed");
        }

        public void OnPostMassDelete()
        {
            InitializeFiles();
            MessageL(COLORS.yellow, $"FileNames/MassDelete method starts, days = {Days}, {TempFiles.Count} temp files");
            if (TempFiles is null) return;
            var permittedDate = DateTime.Now.AddDays(-Days);
            int counter = 0;
            for (int i = 0; i < TempFiles.Count; i++)
            {
                var fileDate = DateTime.Parse(TempFiles[i].Date);
                if (fileDate < permittedDate)
                {
                    var currentPath = Path.Combine(_environment.WebRootPath, "temporary", TempFiles[i].Path);
                    System.IO.File.Delete(currentPath);
                    TempFiles.RemoveAt(i);
                    i--;
                    counter++;
                }
            }
            MessageL(COLORS.cyan, $"{counter} file(s) removed");
            NumberOfFiles = TempFiles.Count + MidiFiles.Count + Mp3Files.Count;
            TotalSize = GetTotalSize();
            Page();
        }

        public async Task<IActionResult> OnPostExport()
        {
            // Collect raw data
            var countries = await _workingContext.Country.AsNoTracking().OrderBy(c => c.ID).ToListAsync();
            var authors = await _workingContext.Author.AsNoTracking().OrderBy(a => a.ID).ToListAsync();
            var melodies = await _workingContext.Melody.AsNoTracking().OrderBy(m => m.ID).ToListAsync();

            // Flatten DTOs preserving FK references by ID for easy import
            var export = new
            {
                ExportVersion = 1,
                GeneratedAtUtc = DateTime.UtcNow,
                Countries = countries.Select(c => new { c.ID, c.Name }),
                Authors = authors.Select(a => new
                {
                    a.ID,
                    a.Surname,
                    a.Name,
                    a.SurnameEn,
                    a.NameEn,
                    a.CountryID,
                    a.DateOfBirth,
                    a.DateOfDeath,
                    a.Description
                }),
                Melodies = melodies.Select(m => new
                {
                    m.ID,
                    m.Title,
                    m.Year,
                    m.Description,
                    m.FilePath,
                    m.IsFileEligible,
                    m.Tonality,
                    m.AuthorID
                })
            };

            var json = JsonSerializer.Serialize(export, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            var bytes = Encoding.UTF8.GetBytes(json);
            var fileName = $"melodies_export_{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
            return File(bytes, "application/json", fileName);
        }
    }
}
