using System.Text.Json;
using Microsoft.Extensions.Options;
using WebApplication3.Model;

namespace WebApplication3.Services;

public interface IRecaptchaVerifier
{
    Task<bool> VerifyAsync(string? token, string? ipAddress);
}

public sealed class RecaptchaVerifier : IRecaptchaVerifier
{
    private readonly HttpClient _httpClient;
    private readonly RecaptchaSettings _settings;
    private readonly IWebHostEnvironment _environment;
    private readonly ILogger<RecaptchaVerifier> _logger;

    public RecaptchaVerifier(
        HttpClient httpClient,
        IOptions<RecaptchaSettings> settings,
        IWebHostEnvironment environment,
        ILogger<RecaptchaVerifier> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _environment = environment;
        _logger = logger;
    }

    public async Task<bool> VerifyAsync(string? token, string? ipAddress)
    {
        if (!_settings.Enabled)
        {
            return true;
        }

        if (_settings.BypassInDevelopment && _environment.IsDevelopment())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var values = new Dictionary<string, string>
        {
            ["secret"] = _settings.SecretKey,
            ["response"] = token
        };

        if (!string.IsNullOrWhiteSpace(ipAddress))
        {
            values["remoteip"] = ipAddress;
        }

        try
        {
            using var content = new FormUrlEncodedContent(values);
            using var response = await _httpClient.PostAsync(_settings.VerifyUrl, content);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Recaptcha verification failed with status {StatusCode}", response.StatusCode);
                return false;
            }

            var payload = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<RecaptchaResponse>(payload);

            return result is { Success: true } && result.Score >= _settings.ScoreThreshold;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recaptcha verification failed");
            return false;
        }
    }

    private sealed class RecaptchaResponse
    {
        public bool Success { get; set; }
        public double Score { get; set; }
    }
}
