# TikTok module

This module is registered with `AddTikTokModule` and owns its `/tiktok` Razor page, MVC API, job queue, and private files in `App_Data/TikTok`.

## Local setup (Windows)

1. Install .NET SDK 6.0.428 for this project. .NET 6 no longer receives security updates; plan an upgrade to a supported .NET LTS before production deployment.
2. Install yt-dlp and FFmpeg. Set `TikTokTools:YtDlpPath`, `TikTokTools:FFmpegPath`, and `TikTokTools:FFprobePath` to their executable locations; the repository config contains this workstation's paths.
3. Check `appsettings.json` paths. Relative paths resolve from the application content root, not the terminal's current directory.
4. Run `dotnet restore`, `dotnet build`, then `dotnet run` from the repository root and open `/tiktok`.
5. Submit an HTTPS TikTok video URL, review its metadata, confirm you own or have permission to use the video, and start the download.

The queue and job ownership live in application memory, so a restart clears job history. Completed MP4 files are private, remain outside `wwwroot`, and are served only through the owner-checked video endpoint. Queue capacity, max duration, file size, retention, AI resolution and mask dilation are configurable in `TikTokTools`.

## Current implementation boundary

Metadata lookup and original MP4 download are implemented through yt-dlp. The worker reports yt-dlp's actual download percentage, supports cancellation, and limits simultaneous downloads. For manual AI inpainting, select a rectangle on the thumbnail; the mask is applied to every frame, then the ProPainter output is muxed with the source audio by FFmpeg and validated with FFprobe. Put `raft-things.pth`, `recurrent_flow_completion.pth`, and `ProPainter.pth` either in ProPainter's own `weights` folder or in `App_Data/TikTok/ProPainterRuntime/weights` before starting an AI job. ProPainter's unmodified CLI downloads its official release checkpoints when missing; this module checks first and does not start those large downloads. The configured Python interpreter must have PyTorch, torchvision, OpenCV, scipy, imageio and ProPainter dependencies installed. `PythonAI/propainter_compat.py` supplies OpenCV-based video decoding for torchvision builds that no longer expose `torchvision.io.read_video`. Automatic watermark detection and masks that track a moving logo are not implemented; the manual selection is a static region. CPU inference works but is very slow: on this workstation, a 25-frame 432×240 sample took about 3 minutes 6 seconds for five ProPainter iterations. The configured PyTorch build is CPU-only; a CUDA-enabled PyTorch setup is recommended for practical use. To reduce waiting on CPU, this workstation defaults to `AiMaximumDimension: 256`; this also reduces the final video's resolution. Set it to `432` for higher output resolution at a longer processing time. The machine has an NVIDIA GeForce MX330 with 2 GB VRAM, but the installed PyTorch build is CPU-only and the GPU has limited memory.
