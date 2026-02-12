namespace WebApplication3.Services;

public interface IRecoveryCodeService
{
    /// <summary>
    /// Generates new recovery codes for a user. Returns plaintext codes (only time they're visible).
    /// </summary>
    Task<string[]> GenerateCodesAsync(string userId, int count, string? ipAddress = null);

    /// <summary>
    /// Validates and redeems a recovery code. Returns true if successful.
    /// </summary>
    Task<bool> ValidateAndRedeemAsync(string userId, string code, string? ipAddress = null);

    /// <summary>
    /// Gets the count of remaining (unused) recovery codes for a user.
    /// </summary>
    Task<int> GetRemainingCodesCountAsync(string userId);

    /// <summary>
    /// Gets all unused recovery codes for a user (for display after generation).
    /// </summary>
    Task<string[]> GetUnusedCodesAsync(string userId);

    /// <summary>
    /// Invalidates all existing recovery codes for a user.
    /// </summary>
    Task InvalidateAllCodesAsync(string userId);
}
