using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace WebApplication3.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // X-Content-Type-Options: prevents MIME type sniffing
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";

        // X-Frame-Options: prevents clickjacking
        context.Response.Headers["X-Frame-Options"] = "DENY";

        // X-XSS-Protection: enables browser XSS filter (legacy but still useful)
        context.Response.Headers["X-XSS-Protection"] = "1; mode=block";

        // Referrer-Policy: controls referrer information
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";

        // Permissions-Policy: controls browser features
        context.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=(), payment=(), usb=(), magnetometer=(), gyroscope=(), speaker=()";

        // Content-Security-Policy: comprehensive XSS and data injection protection
        var csp = "default-src 'self'; " +
                    "script-src 'self' https://www.google.com/recaptcha/ https://www.gstatic.com/recaptcha/; " +
                    "style-src 'self' https://fonts.googleapis.com 'unsafe-inline'; " +
                    "font-src 'self' https://fonts.gstatic.com; " +
                    "img-src 'self' data: blob:; " +
                    "connect-src 'self'; " +
                    "frame-src https://www.google.com/recaptcha/; " +
                    "frame-ancestors 'none'; " +
                    "base-uri 'self'; " +
                    "form-action 'self'; " +
                    "upgrade-insecure-requests;";

        context.Response.Headers["Content-Security-Policy"] = csp;

        await _next(context);
    }
}
