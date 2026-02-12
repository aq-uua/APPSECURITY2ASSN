using Microsoft.EntityFrameworkCore;
using WebApplication3.Model;

namespace WebApplication3.Services;

public sealed class RecoveryCodeService : IRecoveryCodeService
{
    private readonly AuthDbContext _context;
    private readonly ILogger<RecoveryCodeService> _logger;

    public RecoveryCodeService(AuthDbContext context, ILogger<RecoveryCodeService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<string[]> GenerateCodesAsync(string userId, int count, string? ipAddress = null)
    {
        // Invalidate existing codes first
        await InvalidateAllCodesAsync(userId);

        var codes = new List<string>();
        var entities = new List<RecoveryCode>();

        for (int i = 0; i < count; i++)
        {
            var code = GenerateRandomCode();
            var codeHash = HashCode(code);

            entities.Add(new RecoveryCode
            {
                UserId = userId,
                CodeHash = codeHash,
                IsUsed = false,
                CreatedAt = DateTime.UtcNow
            });

            codes.Add(code);
        }

        _context.RecoveryCodes.AddRange(entities);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Generated {Count} recovery codes for user {UserId}", count, userId);

        return codes.ToArray();
    }

    public async Task<bool> ValidateAndRedeemAsync(string userId, string code, string? ipAddress = null)
    {
        var normalizedCode = code.Replace("-", "").Replace(" ", "").ToUpperInvariant();
        var codeHash = HashCode(normalizedCode);

        var recoveryCode = await _context.RecoveryCodes
            .FirstOrDefaultAsync(rc => rc.UserId == userId && 
                                       rc.CodeHash == codeHash && 
                                       !rc.IsUsed);

        if (recoveryCode == null)
        {
            _logger.LogWarning("Invalid or already used recovery code attempt for user {UserId}", userId);
            return false;
        }

        recoveryCode.IsUsed = true;
        recoveryCode.UsedAt = DateTime.UtcNow;
        recoveryCode.UsedFromIp = ipAddress?.Length > 50 ? ipAddress[..50] : ipAddress;

        await _context.SaveChangesAsync();

        _logger.LogInformation("Recovery code redeemed successfully for user {UserId}", userId);
        return true;
    }

    public async Task<int> GetRemainingCodesCountAsync(string userId)
    {
        return await _context.RecoveryCodes
            .CountAsync(rc => rc.UserId == userId && !rc.IsUsed);
    }

    public async Task<string[]> GetUnusedCodesAsync(string userId)
    {
        // This method returns codes from the database
        // Note: We only store hashes, so we can't return the actual codes
        // This is by design - codes are only visible when first generated
        var count = await GetRemainingCodesCountAsync(userId);
        return new string[count];
    }

    public async Task InvalidateAllCodesAsync(string userId)
    {
        var existingCodes = await _context.RecoveryCodes
            .Where(rc => rc.UserId == userId)
            .ToListAsync();

        if (existingCodes.Any())
        {
            _context.RecoveryCodes.RemoveRange(existingCodes);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Invalidated {Count} old recovery codes for user {UserId}", 
                existingCodes.Count, userId);
        }
    }

    private static string GenerateRandomCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        var code = new char[10];

        for (int i = 0; i < 10; i++)
        {
            code[i] = chars[random.Next(chars.Length)];
        }

        // Format as XXXXX-XXXXX
        return new string(code, 0, 5) + "-" + new string(code, 5, 5);
    }

    private static string HashCode(string code)
    {
        // Simple hash for demonstration - in production use proper hashing
        using var sha256 = System.Security.Cryptography.SHA256.Create();
        var bytes = System.Text.Encoding.UTF8.GetBytes(code);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }
}
