using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;

public class TapPageModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int TapId { get; set; }

    public string? UserId { get; set; }

    public string Message { get; set; } = "";

    public IActionResult OnGet(string? returnUrl)
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            // Redirect to login, passing the returnUrl
            return RedirectToPage("/Index", new { returnUrl = returnUrl ?? $"/Tap/{TapId}" });
        }

        return Page();
    }

    public IActionResult OnPostPourBeer()
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            return RedirectToPage("/Index", new { returnUrl = $"/Tap/{TapId}" });
        }

        // Simulate the pour action (replace with hardware logic)
        Message = $"Beer poured on tap {TapId} for user {UserId}.";
        return Page();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("UserId");
        return RedirectToPage("/Index");
    }
}
