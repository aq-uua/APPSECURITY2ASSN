using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using WebApplication3.Middleware;
using WebApplication3.Model;
using WebApplication3.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages(options =>
{
    options.Conventions.ConfigureFilter(new AutoValidateAntiforgeryTokenAttribute());
    options.Conventions.AddPageApplicationModelConvention("/Login", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"));
    });

    options.Conventions.AddPageApplicationModelConvention("/Register", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"));
    });

    options.Conventions.AddPageApplicationModelConvention("/ForgotPassword", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"));
    });

    options.Conventions.AddPageApplicationModelConvention("/ResetPassword", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"));
    });

    options.Conventions.AddPageApplicationModelConvention("/LoginWith2fa", model =>
    {
        model.EndpointMetadata.Add(new EnableRateLimitingAttribute("auth"));
    });
});
builder.Services.AddDbContext<AuthDbContext>();
builder.Services.Configure<SessionSettings>(builder.Configuration.GetSection("SessionSettings"));
builder.Services.Configure<PasswordPolicySettings>(builder.Configuration.GetSection("PasswordPolicy"));
builder.Services.Configure<RecaptchaSettings>(builder.Configuration.GetSection("Recaptcha"));
builder.Services.Configure<RecoverySettings>(builder.Configuration.GetSection("RecoverySettings"));

// Configure Identity with ApplicationUser
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 12;
    options.Password.RequiredUniqueChars = 1;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;

    // SignIn settings
    options.SignIn.RequireConfirmedEmail = true;
})
.AddEntityFrameworkStores<AuthDbContext>()
.AddDefaultTokenProviders();

builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    var minutes = builder.Configuration.GetValue<int>("RecoverySettings:PasswordResetTokenMinutes", 30);
    options.TokenLifespan = TimeSpan.FromMinutes(minutes);
});



// Configure Application Cookie
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Login";
    options.LogoutPath = "/Logout";
    options.AccessDeniedPath = "/AccessDenied";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
    options.SlidingExpiration = true;
});

builder.Services.AddSession(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.IdleTimeout = TimeSpan.FromMinutes(builder.Configuration.GetValue<int>("SessionSettings:TimeoutMinutes", 30));
});

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 5 * 1024 * 1024;
});

builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 5 * 1024 * 1024;
});

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogWarning("Rate limit hit for {Path} from {Ip}", context.HttpContext.Request.Path, context.HttpContext.Connection.RemoteIpAddress);
        context.HttpContext.Response.Headers.RetryAfter = "60";
        return ValueTask.CompletedTask;
    };

    options.AddPolicy("auth", httpContext =>
    {
        var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetSlidingWindowLimiter(ip, _ => new SlidingWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            SegmentsPerWindow = 2,
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0
        });
    });
});

// Add Data Protection
builder.Services.AddDataProtection()
    .SetApplicationName("FarmFreshMarket")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

// Add Email Sender
builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddHttpClient<IRecaptchaVerifier, RecaptchaVerifier>();
// Razor view renderer for email templates
builder.Services.AddSingleton<IRazorViewToStringRenderer, RazorViewToStringRenderer>();
builder.Services.AddScoped<IAuditLogger, AuditLogger>();
builder.Services.AddScoped<ISessionManager, SessionManager>();
builder.Services.AddHostedService<SessionCleanupService>();

// Add HSTS (HTTP Strict Transport Security)
builder.Services.AddHsts(options =>
{
    options.Preload = true;
    options.IncludeSubDomains = true;
    options.MaxAge = TimeSpan.FromDays(365);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();
app.UseMiddleware<SessionActivityMiddleware>();
app.UseAuthorization();

app.UseStatusCodePagesWithReExecute("/StatusCode", "?statusCode={0}");

app.MapRazorPages();

app.Run();
