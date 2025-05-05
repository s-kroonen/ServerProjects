using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class HomeModel : PageModel
{
    public string? LoggedInUserId { get; set; }

    public List<int> AvailableTaps { get; set; } = new List<int> { 1, 2, 3 }; // Example tap list

    public IActionResult OnGet()
    {
        var userId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(userId))
        {
            return RedirectToPage("/Index", new { returnUrl = "/Home" });
        }

        LoggedInUserId = userId;
        return Page();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("UserId");
        return RedirectToPage("/Index");
    }
}
