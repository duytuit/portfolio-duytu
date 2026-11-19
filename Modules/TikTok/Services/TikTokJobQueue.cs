using System.Threading.Channels;
using PortfolioMvc.Modules.TikTok.Models;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class TikTokJobQueue
{
    private readonly Channel<TikTokJobState> channel;

    public TikTokJobQueue(int capacity)
    {
        channel = Channel.CreateBounded<TikTokJobState>(new BoundedChannelOptions(Math.Clamp(capacity, 1, 100))
        {
            FullMode = BoundedChannelFullMode.Wait,
            SingleReader = false,
            SingleWriter = false
        });
    }

    public bool TryEnqueue(TikTokJobState state) => channel.Writer.TryWrite(state);
    public ValueTask<TikTokJobState> DequeueAsync(CancellationToken cancellationToken) =>
        channel.Reader.ReadAsync(cancellationToken);
}
