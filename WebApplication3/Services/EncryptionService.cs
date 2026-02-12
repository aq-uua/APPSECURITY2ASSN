using Microsoft.AspNetCore.DataProtection;

namespace WebApplication3.Services;

public sealed class EncryptionService : IEncryptionService
{
    private const string ProtectorPurpose = "WebApplication3.SensitiveData.v1";
    private readonly IDataProtector _protector;

    public EncryptionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector(ProtectorPurpose);
    }

    public string Protect(string plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText))
        {
            return string.Empty;
        }

        return _protector.Protect(plainText);
    }

    public string Unprotect(string protectedText)
    {
        if (string.IsNullOrWhiteSpace(protectedText))
        {
            return string.Empty;
        }

        return _protector.Unprotect(protectedText);
    }
}
