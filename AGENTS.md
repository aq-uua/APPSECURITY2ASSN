# AGENTS.md - Coding Guidelines for Farm Fresh Market

## Project Overview

Farm Fresh Market is a secure ASP.NET Core 8 Razor Pages application for membership/registration. It uses Entity Framework Core 8 with Identity, SQL Server LocalDB, and implements comprehensive security features (encryption, password policies, rate limiting).

## Build Commands

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run the application
dotnet run --project WebApplication3

# Run with specific URLs
dotnet run --project WebApplication3 --urls "https://localhost:5001;http://localhost:5000"

# Database migrations
dotnet ef migrations add <MigrationName> --project WebApplication3 --context AuthDbContext
dotnet ef database update --project WebApplication3 --context AuthDbContext

# Watch mode for development
dotnet watch run --project WebApplication3
```

**Note:** No test projects currently exist. When adding tests, use xUnit with `dotnet test`.

## Code Style Guidelines

### General C# Conventions

- Use **file-scoped namespaces** (no braces needed)
- Enable **nullable reference types** (`<Nullable>enable</Nullable>`)
- Use **implicit usings** (`<ImplicitUsings>enable</ImplicitUsings>`)
- Target **.NET 8.0** with latest C# features

### Naming Conventions

- **PascalCase**: Classes, methods, properties, public fields, enums
- **camelCase**: Private fields, local variables, parameters
- **_camelCase**: Private fields with underscore prefix
- **ALL_CAPS**: Constants

Examples:
```csharp
public class ApplicationUser : IdentityUser  // PascalCase
{
    private readonly UserManager<ApplicationUser> _userManager;  // _camelCase
    public string FullName { get; set; } = string.Empty;  // PascalCase properties
    private const int MAX_RETRY_ATTEMPTS = 5;  // ALL_CAPS
}
```

### Imports & Organization

```csharp
// 1. System namespaces
using System;
using System.ComponentModel.DataAnnotations;

// 2. Microsoft namespaces
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

// 3. Third-party packages (if any)

// 4. Project namespaces
using WebApplication3.Model;
using WebApplication3.Services;
using WebApplication3.ViewModels;
```

### Type Safety

- Always use nullable annotations: `string?` for nullable, `string` for required
- Initialize non-nullable strings with `= string.Empty;`
- Use `var` only when type is obvious from right-hand side
- Prefer `async/await` over blocking calls

### Error Handling

```csharp
// Use try-catch for external operations (database, email, file I/O)
try
{
    await _emailSender.SendEmailAsync(user.Email, subject, message);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Failed to send email to: {Email}", user.Email);
    throw;  // Re-throw or handle appropriately
}

// Use Result pattern for Identity operations
var result = await _userManager.CreateAsync(user, password);
if (!result.Succeeded)
{
    foreach (var error in result.Errors)
    {
        ModelState.AddModelError(string.Empty, error.Description);
    }
}
```

### Razor Pages Patterns

- Use `[BindProperty]` for form data binding
- Use `[BindProperty(SupportsGet = true)]` for query parameters
- Implement `OnGet()`, `OnPostAsync()`, `OnGet/Post<Handler>()` methods
- Return `Page()` to redisplay form with errors
- Return `RedirectToPage("PageName")` for successful operations

### Security Requirements

**CRITICAL: Follow these security patterns:**

1. **Never store raw credit card data** - encrypt or use tokenization
2. **Use proper encryption placeholder** for sensitive data:
   ```csharp
   string encryptedData = EncryptSensitiveData(rawData);
   ```
3. **Validate all inputs server-side** - client validation is not enough
4. **Use parameterized queries** - EF Core prevents SQL injection
5. **Sanitize file uploads**:
   - Validate MIME type (JPG only)
   - Use safe filenames with GUIDs
   - Restrict file size (5MB max)
6. **Password policy**: Min 12 chars, require upper/lower/number/special
7. **Secure cookies**: HttpOnly, Secure, SameSite=Strict
8. **Rate limiting**: Max 5 failed attempts before lockout
9. **Audit logging**: Log all auth events (success/failure)

### Entity Framework Patterns

- Use `DbContext` with connection string from config
- Use `[Required]`, `[StringLength]` data annotations
- Store UTC times: `DateTime.UtcNow`
- Use migrations for schema changes
- Never expose raw database errors to users

### Logging

```csharp
// Use structured logging
_logger.LogInformation("User created: {Email}", user.Email);
_logger.LogError(ex, "Operation failed: {Operation}", operationName);
_logger.LogWarning("Suspicious activity from {IpAddress}", ipAddress);
```

### Configuration

- Store secrets in `appsettings.json` (dev) or environment variables (prod)
- Use strongly-typed configuration sections
- Never commit passwords or API keys to repository

## Project Structure

```
WebApplication3/
├── Model/              # Entities (ApplicationUser, DbContext)
├── ViewModels/         # Page models (Register, Login)
├── Pages/              # Razor Pages (.cshtml + .cshtml.cs)
├── Services/           # Business logic (EmailSender)
├── Migrations/         # EF Core migrations
├── wwwroot/            # Static files
└── appsettings.json    # Configuration
```

## Key Technologies

- **Framework**: ASP.NET Core 8 Razor Pages
- **ORM**: Entity Framework Core 8
- **Database**: SQL Server LocalDB (dev)
- **Identity**: ASP.NET Core Identity with custom ApplicationUser
- **Security**: Data Protection API, Identity password hashing

## Compliance Notes

- PCI-DSS considerations for credit card handling
- GDPR compliance for personal data (right to deletion)
- TLS 1.2+ enforcement in production
- Never log sensitive data (passwords, credit cards)

---

**Always run `dotnet build` before committing changes to ensure no compilation errors.**
