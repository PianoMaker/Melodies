using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using Melodies25.Data;

public class SyncModel : PageModel
{
    private readonly DatabaseSyncService _syncService;
    public IReadOnlyList<string> Collisions { get; private set; } = Array.Empty<string>();
    public bool Done { get; private set; }

    public SyncModel(DatabaseSyncService syncService)
    {
        _syncService = syncService;
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        Collisions = await _syncService.SyncDatabasesAsync();
        Done = true;
        return Page();
    }
}