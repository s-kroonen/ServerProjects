using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class TapPageModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public int TapId { get; set; }

    public string? UserId { get; set; }

    public string Message { get; set; } = "";

    public IActionResult OnGet()
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            return RedirectToPage("/Index", new { returnUrl = $"/Tap/{TapId}" });
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

        // Here you'd trigger the actual hardware/API to pour beer.
        // For now, we simulate.
        Message = $"Beer poured on tap {TapId} for user {UserId}.";

        return Page();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("UserId");
        return RedirectToPage("/Index");
    }
}
