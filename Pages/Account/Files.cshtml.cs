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

        public void OnPostDeleteFile(int Id)
        {

            MessageL(COLORS.yellow, "FileNames/DeleteFile method starts");
            var temporarifiles = Path.Combine(_environment.WebRootPath, "temporary");

            TempFiles.RemoveAt(Id);
            MessageL(COLORS.yellow, "file removed");

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
