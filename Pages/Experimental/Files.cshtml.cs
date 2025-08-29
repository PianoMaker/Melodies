using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.DotNet.Scaffolding.Shared;
using Music;
using System.Runtime.CompilerServices;
using static Music.Messages;


namespace Melodies25.Pages.Account
{
    [BindProperties] // Move the attribute to the class level
    public class FilesModel : PageModel
    {
        public List<FileInformation>? TempFiles { get; set; } = new List<FileInformation>();
        public List<FileInformation>? MidiFiles { get; set; } = new List<FileInformation>();
        public List<FileInformation>? Mp3Files { get; set; } = new List<FileInformation>();

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
        

        public IWebHostEnvironment _environment;

        public FilesModel(IWebHostEnvironment environment) =>
        _environment = environment;

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
                var FileNames = Directory.GetFiles(fileslist)
                    .Select(Path.GetFileName)
                    .ToList();

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

        public IActionResult OnPostDeleteFile(string fileId)
        {
            MessageL(COLORS.yellow, "FileNames/DeleteFile method starts");

            if (string.IsNullOrEmpty(fileId))
            {
                ErrorMessageL("File ID is null or empty");
                return Page();
            }

            // Ініціалізуємо файли
            InitializeFiles();

            // Парсимо ID файлу
            if (!int.TryParse(fileId, out int id))
            {
                ErrorMessageL("Invalid file ID format");
                return Page();
            }

            try
            {
                // Шукаємо файл у всіх колекціях
                FileInformation? fileToDelete = null;
                string? subcategory = null;

                // Перевіряємо у TempFiles
                if (TempFiles != null && id < TempFiles.Count)
                {
                    fileToDelete = TempFiles[id];
                    subcategory = "temporary";
                }
                // Перевіряємо у MidiFiles  
                else if (MidiFiles != null && id < MidiFiles.Count)
                {
                    fileToDelete = MidiFiles[id];
                    subcategory = "melodies";
                }
                // Перевіряємо у Mp3Files
                else if (Mp3Files != null && id < Mp3Files.Count)
                {
                    fileToDelete = Mp3Files[id];
                    subcategory = "mp3";
                }

                if (fileToDelete == null || subcategory == null)
                {
                    ErrorMessageL("File not found in any collection");
                    return Page();
                }

                // Створюємо повний шлях до файлу
                var filePath = Path.Combine(_environment.WebRootPath, subcategory, fileToDelete.Value.Name);

                // Перевіряємо чи існує файл
                if (System.IO.File.Exists(filePath))
                {
                    // Видаляємо фізичний файл
                    System.IO.File.Delete(filePath);
                    MessageL(COLORS.green, $"Physical file deleted: {filePath}");
                }
                else
                {
                    MessageL(COLORS.yellow, $"Physical file not found: {filePath}");
                }

                // Видаляємо з відповідної колекції
                if (subcategory == "temporary")
                {
                    TempFiles?.RemoveAt(id);
                }
                else if (subcategory == "melodies")
                {
                    MidiFiles?.RemoveAt(id);
                }
                else if (subcategory == "mp3")
                {
                    Mp3Files?.RemoveAt(id);
                }

                MessageL(COLORS.cyan, "File removed successfully");

                // Оновлюємо статистику
                NumberOfFiles = (TempFiles?.Count ?? 0) + (MidiFiles?.Count ?? 0) + (Mp3Files?.Count ?? 0);
                TotalSize = GetTotalSize();
            }
            catch (Exception ex)
            {
                ErrorMessageL($"Error deleting file: {ex.Message}");
            }

            return Page();
        }

        public void OnPostMassDelete()
        {
            InitializeFiles();
            MessageL(COLORS.yellow, $"FileNames/MassDelete method starts, days = {Days}, {TempFiles.Count} temp files");

            if (TempFiles is null) return;

            var dateNow = DateTime.Now;
            var permittedDate = dateNow.AddDays(-Days); // Fixes both CS1955 and CS0019
            int counter = 0;

            for (int i = 0; i < TempFiles.Count; i++)
            {
                var fileDate = DateTime.Parse(TempFiles[i].Date);                
                if (fileDate < permittedDate)
                {
                    var currentPath = Path.Combine(_environment.WebRootPath, "temporary", TempFiles[i].Path);
                    System.IO.File.Delete(currentPath); 
                    //GrayMessageL($"{currentPath}: {fileDate} vs {permittedDate}");
                    TempFiles.RemoveAt(i);
                    i--; // Adjust index after removal
                    counter++;
                }
            }

            MessageL(COLORS.cyan, $"{counter} file(s) removed");
            NumberOfFiles = TempFiles.Count + MidiFiles.Count + Mp3Files.Count;
            TotalSize = GetTotalSize();

            Page();
        }
    }
}
