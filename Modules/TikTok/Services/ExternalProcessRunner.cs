using System.ComponentModel;
using System.Diagnostics;

namespace PortfolioMvc.Modules.TikTok.Services;

public sealed class ExternalProcessRunner
{
    private readonly ILogger<ExternalProcessRunner> logger;

    public ExternalProcessRunner(ILogger<ExternalProcessRunner> logger) => this.logger = logger;

    public async Task<ExternalProcessResult> RunAsync(string executable, IEnumerable<string> arguments,
        string workingDirectory, CancellationToken cancellationToken, Action<string>? onOutputLine = null,
        TimeSpan? timeout = null)
    {
        var start = new ProcessStartInfo
        {
            FileName = executable,
            WorkingDirectory = workingDirectory,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
        foreach (var argument in arguments) start.ArgumentList.Add(argument);

        using var process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new TikTokToolException($"Không thể khởi chạy công cụ {Path.GetFileName(executable)}.");
        }
        catch (Win32Exception exception)
        {
            logger.LogWarning(exception, "Could not start external tool {Tool}", Path.GetFileName(executable));
            throw new TikTokToolException($"Không tìm thấy hoặc không thể khởi chạy {Path.GetFileName(executable)}.", exception);
        }

        var stdout = new System.Text.StringBuilder();
        var stderr = new System.Text.StringBuilder();
        var stdoutTask = ReadLinesAsync(process.StandardOutput, stdout, onOutputLine);
        var stderrTask = ReadLinesAsync(process.StandardError, stderr, onOutputLine);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout ?? TimeSpan.FromMinutes(30));
        try
        {
            await process.WaitForExitAsync(linked.Token);
        }
        catch (OperationCanceledException)
        {
            if (!process.HasExited)
            {
                try { process.Kill(entireProcessTree: true); }
                catch (InvalidOperationException) { }
                await process.WaitForExitAsync(CancellationToken.None);
            }
            await Task.WhenAll(stdoutTask, stderrTask);
            if (!cancellationToken.IsCancellationRequested)
                throw new TikTokToolException($"Công cụ {Path.GetFileName(executable)} vượt quá thời gian xử lý cho phép.");
            throw;
        }

        await Task.WhenAll(stdoutTask, stderrTask);
        return new ExternalProcessResult(process.ExitCode, stdout.ToString(), stderr.ToString());
    }

    private static async Task ReadLinesAsync(StreamReader reader, System.Text.StringBuilder capture, Action<string>? onLine)
    {
        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            if (capture.Length < 128_000) capture.AppendLine(line);
            onLine?.Invoke(line);
        }
    }
}

public sealed record ExternalProcessResult(int ExitCode, string Stdout, string Stderr);
