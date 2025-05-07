using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string? LoginUserId { get; set; }

    [BindProperty]
    public string? RegisterUserId { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public bool IsLoggedIn => HttpContext.Session.GetString("UserId") != null;

    public IActionResult OnGet()
    {
        if (IsLoggedIn)
        {
            return Redirect(ReturnUrl ?? "/Home");
        }

        return Page();
    }

    public IActionResult OnPostLogin(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(LoginUserId))
        {
            HttpContext.Session.SetString("UserId", LoginUserId);
        }

        return Redirect(returnUrl ?? "/Home");
    }

    public IActionResult OnPostRegister(string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(RegisterUserId))
        {
            HttpContext.Session.SetString("UserId", RegisterUserId);
        }

        return Redirect(returnUrl ?? "/Home");
    }
}
