using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebApplication3.Pages;

[IgnoreAntiforgeryToken]
public class StatusCodeModel : PageModel
{
    public string Title { get; private set; } = "Request error";
    public string Message { get; private set; } = "We couldn't process your request.";

    public void OnGet(int statusCode)
    {
        (Title, Message) = statusCode switch
        {
            403 => ("Access denied", "You don't have permission to view this page."),
            404 => ("Page not found", "We couldn't find the page you're looking for."),
            429 => ("Too many requests", "Please slow down and try again shortly."),
            _ => ("Request error", "We couldn't process your request.")
        };
    }
}
