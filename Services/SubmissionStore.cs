using System.Text.Json;
using PortfolioMvc.Models;
namespace PortfolioMvc.Services;
// Store outside wwwroot: submissions must never be exposed as static assets.
public sealed class SubmissionStore {
    private readonly string directory;
    private readonly SemaphoreSlim gate = new(1, 1);
    public SubmissionStore(IWebHostEnvironment env) { directory = Path.Combine(env.ContentRootPath, "App_Data"); }
    public Task SaveContact(ContactForm form) => Append("contacts.jsonl", new { form.Name, form.Email, form.Subject, form.Message, CreatedAt = DateTimeOffset.UtcNow });
    public Task SaveSubscriber(NewsletterForm form) => Append("subscribers.jsonl", new { form.Email, CreatedAt = DateTimeOffset.UtcNow });
    private async Task Append(string file, object value) {
        await gate.WaitAsync();
        try { Directory.CreateDirectory(directory); await File.AppendAllTextAsync(Path.Combine(directory, file), JsonSerializer.Serialize(value) + Environment.NewLine); }
        finally { gate.Release(); }
    }
}
