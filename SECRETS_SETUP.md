# Secrets Configuration Guide

## Overview

For security reasons, sensitive configuration values (passwords, API keys, etc.) have been removed from `appsettings.json`. This guide explains how to configure these secrets securely for local development and production.

## High-Priority Security Fix

**IMPORTANT**: The following secrets were removed from source control to fix HIGH IMPORTANCE security vulnerabilities:
- SMTP email credentials (username and password)
- reCAPTCHA API keys (site key and secret key)

## Local Development Setup

### Using .NET User Secrets (Recommended)

User Secrets is a secure way to store sensitive configuration data during development. The secrets are stored outside your project directory and never committed to source control.

#### 1. Initialize User Secrets

```bash
cd WebApplication3
dotnet user-secrets init
```

This adds a `UserSecretsId` to your `.csproj` file (already done if present).

#### 2. Set Email Configuration

```bash
dotnet user-secrets set "Email:SmtpUsername" "your-email@gmail.com"
dotnet user-secrets set "Email:SmtpPassword" "your-app-specific-password"
dotnet user-secrets set "Email:FromEmail" "your-email@gmail.com"
```

**Note for Gmail users**: 
- Use an [App Password](https://support.google.com/accounts/answer/185833) instead of your regular password
- Enable 2-factor authentication on your Google account first
- Generate an App Password specifically for this application

#### 3. Set reCAPTCHA Configuration (Optional)

If you want to enable reCAPTCHA:

```bash
dotnet user-secrets set "Recaptcha:SiteKey" "your-recaptcha-site-key"
dotnet user-secrets set "Recaptcha:SecretKey" "your-recaptcha-secret-key"
dotnet user-secrets set "Recaptcha:Enabled" "true"
```

Get your reCAPTCHA keys from: https://www.google.com/recaptcha/admin

#### 4. Verify Your Secrets

```bash
dotnet user-secrets list
```

### Alternative: Environment Variables

You can also use environment variables:

**Windows (PowerShell):**
```powershell
$env:Email__SmtpUsername="your-email@gmail.com"
$env:Email__SmtpPassword="your-app-password"
$env:Email__FromEmail="your-email@gmail.com"
```

**Linux/macOS:**
```bash
export Email__SmtpUsername="your-email@gmail.com"
export Email__SmtpPassword="your-app-password"
export Email__FromEmail="your-email@gmail.com"
```

Note: Use double underscores (`__`) to represent nested configuration in environment variables.

## Production Deployment

### Azure App Service

1. Go to your App Service in Azure Portal
2. Navigate to **Configuration** > **Application settings**
3. Add the following settings:
   - `Email__SmtpUsername`
   - `Email__SmtpPassword`
   - `Email__FromEmail`
   - `Recaptcha__SiteKey`
   - `Recaptcha__SecretKey`
   - `Recaptcha__Enabled`

### Docker

Use environment variables or Docker secrets:

```bash
docker run -e Email__SmtpUsername="your-email" \
           -e Email__SmtpPassword="your-password" \
           your-image
```

Or use a `.env` file (NOT committed to source control):

```bash
docker run --env-file .env.production your-image
```

### Other Platforms

Most hosting platforms (AWS, Heroku, Railway, etc.) provide secure environment variable configuration through their dashboards or CLI tools.

## Security Best Practices

✅ **DO:**
- Use User Secrets for local development
- Use environment variables or secure vaults in production
- Use app-specific passwords (not your main email password)
- Rotate credentials regularly
- Use different credentials for development and production

❌ **DON'T:**
- Commit secrets to source control
- Share credentials in chat or email
- Use production credentials in development
- Store secrets in plain text files
- Hard-code secrets in your application code

## Troubleshooting

### "Email configuration is missing" error

This means the application cannot find your email credentials. Verify:
1. User secrets are set correctly: `dotnet user-secrets list`
2. Environment variables are set (if using that method)
3. The secret names match exactly (case-sensitive)

### Email sending fails

1. Verify your SMTP credentials are correct
2. For Gmail, ensure you're using an App Password, not your regular password
3. Check that 2FA is enabled on your Google account
4. Verify SMTP server and port are correct (smtp.gmail.com:587 for Gmail)

### reCAPTCHA not working

1. Verify you're using the correct site key and secret key
2. Check that your domain is registered in reCAPTCHA admin console
3. Ensure `Recaptcha:Enabled` is set to `true`

## Required Secrets

### Minimum Configuration (Required)

```bash
# Email settings (required for registration and password reset)
Email:SmtpUsername
Email:SmtpPassword
Email:FromEmail
```

### Optional Configuration

```bash
# reCAPTCHA (optional, for bot protection)
Recaptcha:SiteKey
Recaptcha:SecretKey
Recaptcha:Enabled

# Other settings use defaults from appsettings.json
```

## Development Testing

For local testing without real email, consider using:
- [smtp4dev](https://github.com/rnwood/smtp4dev) - Local SMTP server
- [MailHog](https://github.com/mailhog/MailHog) - Email testing tool
- [Mailtrap](https://mailtrap.io/) - Email sandbox service

Configure User Secrets to point to these services:

```bash
dotnet user-secrets set "Email:SmtpServer" "localhost"
dotnet user-secrets set "Email:SmtpPort" "25"
dotnet user-secrets set "Email:SmtpUsername" ""
dotnet user-secrets set "Email:SmtpPassword" ""
```

## Additional Resources

- [.NET User Secrets Documentation](https://docs.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Gmail App Passwords](https://support.google.com/accounts/answer/185833)
- [reCAPTCHA Admin Console](https://www.google.com/recaptcha/admin)
- [Azure App Service Configuration](https://docs.microsoft.com/en-us/azure/app-service/configure-common)
