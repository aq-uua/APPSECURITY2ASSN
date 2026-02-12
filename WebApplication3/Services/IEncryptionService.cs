namespace WebApplication3.Services;

public interface IEncryptionService
{
    string Protect(string plainText);
    string Unprotect(string protectedText);
}
