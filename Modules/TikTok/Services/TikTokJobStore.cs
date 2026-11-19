using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokJobStore
{
    private readonly ConcurrentDictionary<Guid, TikTokJobState> jobs = new();

    public bool TryAdd(TikTokJobState state) => jobs.TryAdd(state.Job.Id, state);
    public void Remove(Guid id)
    {
        if (jobs.TryRemove(id, out var removed)) removed.Cancellation.Dispose();
    }
    public void Prune(DateTimeOffset cutoff)
    {
        foreach (var pair in jobs)
        {
            var job = pair.Value.Job;
            if (job.CreatedUtc < cutoff && (job.Status is "Completed" or "Failed" or "Cancelled") &&
                jobs.TryRemove(pair.Key, out var removed))
                removed.Cancellation.Dispose();
        }
    }
    public TikTokJobState? GetState(Guid id) => jobs.TryGetValue(id, out var state) ? state : null;

    public TikTokJob? Get(Guid id, string ownerId)
    {
        var state = GetState(id);
        return state is not null && state.OwnerHash == HashOwner(ownerId) ? Snapshot(state.Job) : null;
    }

    public IReadOnlyList<TikTokJob> History(string ownerId) => jobs.Values
        .Where(state => state.OwnerHash == HashOwner(ownerId))
        .OrderByDescending(state => state.Job.CreatedUtc)
        .Select(state => Snapshot(state.Job))
        .ToArray();

    public bool Cancel(Guid id, string ownerId)
    {
        var state = GetState(id);
        if (state is null || state.OwnerHash != HashOwner(ownerId) ||
            state.Job.Status is "Completed" or "Failed" or "Cancelled")
            return false;
        state.Cancellation.Cancel();
        return true;
    }

    public static string HashOwner(string ownerId) => Convert.ToHexString(
        SHA256.HashData(Encoding.UTF8.GetBytes(ownerId)));

    private static TikTokJob Snapshot(TikTokJob job) => new()
    {
        Id = job.Id,
        Status = job.Status,
        Progress = job.Progress,
        Error = job.Error,
        CreatedUtc = job.CreatedUtc,
        CompletedUtc = job.CompletedUtc,
        DownloadUrl = job.DownloadUrl,
        WatermarkRemovalRequested = job.WatermarkRemovalRequested
    };
}
