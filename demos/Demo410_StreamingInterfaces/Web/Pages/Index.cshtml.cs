using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ClientWebAgent.Pages;

/// <summary>Serves the browser shell; the shared backend owns model and conversation state.</summary>
public sealed class IndexModel : PageModel
{
    /// <summary>Renders the client without loading server-side conversation state.</summary>
    public void OnGet() { }
}
