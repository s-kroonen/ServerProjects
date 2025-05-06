using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc;
using BeerTap.Services;

public class TapPageModel : PageModel
{

    private readonly TapQueueManager _queue;

    public TapPageModel(TapQueueManager queue)
    {
        _queue = queue;
    }
    public int QueuePosition { get; set; }

    [BindProperty(SupportsGet = true)]
    public string TapId { get; set; }

    public string? UserId { get; set; }

    public string Message { get; set; } = "";

    public bool CanTap => _queue.IsUserNext(TapId, UserId!);
    public IActionResult OnGet(string? returnUrl)
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            // Redirect to login, passing the returnUrl
            return RedirectToPage("/Index", new { returnUrl = returnUrl ?? $"/Tap/{TapId}" });
        }

        _queue.EnqueueUser(TapId, UserId);
        QueuePosition = _queue.GetUserPosition(TapId.ToString(), UserId);

        return Page();
    }

    public IActionResult OnPostPourBeer()
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (string.IsNullOrEmpty(UserId))
        {
            return RedirectToPage("/Index", new { returnUrl = $"/Tap/{TapId}" });
        }

        // Simulate tap logic (trigger MQTT here if needed)
        _queue.DequeueUser(TapId);

        Message = $"Beer poured on tap {TapId} for user {UserId}.";
        return Page();
    }

    public IActionResult OnPostLogout()
    {
        HttpContext.Session.Remove("UserId");
        return RedirectToPage("/Index");
    }

    public IActionResult OnPostCancelUser()
    {
        UserId = HttpContext.Session.GetString("UserId");
        if (!string.IsNullOrEmpty(UserId))
        {
            _queue.Cancel(TapId, UserId);
        }

        return RedirectToPage("/Home");
    }

}
