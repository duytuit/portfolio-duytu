using System.Text.Json;
using PortfolioMvc.Models;

namespace PortfolioMvc.Services;

public sealed class TelegramNotifier : IDisposable
{
    // Use a dedicated client so request URLs containing the bot token are not logged by HttpClientFactory.
    private readonly HttpClient client = new() { Timeout = TimeSpan.FromSeconds(15) };
    private readonly IConfiguration configuration;
    private readonly ILogger<TelegramNotifier> logger;

    public TelegramNotifier(IConfiguration configuration, ILogger<TelegramNotifier> logger)
    {
        this.configuration = configuration;
        this.logger = logger;
    }

    public async Task<bool> SendContact(ContactForm form)
    {
        var token = configuration["Telegram:BotToken"];
        var chatId = configuration["Telegram:ChatId"];
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(chatId))
        {
            logger.LogWarning("Telegram contact notifications are not configured.");
            return false;
        }

        var text = $"LỜI NHẮN MỚI TỪ WEBSITE\n\nHọ tên: {form.Name}\nEmail: {form.Email}\nTiêu đề: {form.Subject}\n\nNội dung:\n{form.Message}";
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        try
        {
            // Split long submissions without cutting a UTF-16 surrogate pair; Telegram allows 4096 characters.
            for (var offset = 0; offset < text.Length;)
            {
                var length = Math.Min(3500, text.Length - offset);
                if (offset + length < text.Length && char.IsHighSurrogate(text[offset + length - 1])) length--;
                using var body = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["chat_id"] = chatId,
                    ["text"] = text.Substring(offset, length)
                });
                using var response = await client.PostAsync($"https://api.telegram.org/bot{token}/sendMessage", body, timeout.Token);
                if (!response.IsSuccessStatusCode)
                {
                    logger.LogWarning("Telegram notification failed with HTTP status {StatusCode}.", (int)response.StatusCode);
                    return false;
                }
                using var result = JsonDocument.Parse(await response.Content.ReadAsStringAsync(timeout.Token));
                if (!result.RootElement.TryGetProperty("ok", out var ok) || ok.ValueKind != JsonValueKind.True)
                    return false;
                offset += length;
            }
            return true;
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or JsonException)
        {
            // Never log exception details: they can contain the token-bearing URL.
            logger.LogWarning("Telegram notification failed ({FailureType}).", ex.GetType().Name);
            return false;
        }
    }

    public void Dispose() => client.Dispose();
}
