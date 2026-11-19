#!/usr/bin/env python3
"""Validate options and invoke the checked-out ProPainter project's real CLI."""
import argparse
import json
import os
import subprocess
import sys
from pathlib import Path

import cv2


def dimensions(video: Path, maximum: int) -> tuple[int, int]:
    capture = cv2.VideoCapture(str(video))
    if not capture.isOpened():
        raise RuntimeError("OpenCV could not open the input video.")
    width = int(capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    capture.release()
    if width <= 0 or height <= 0:
        raise RuntimeError("Input video dimensions are invalid.")
    scale = min(1.0, maximum / max(width, height))
    out_width = max(64, int(width * scale) // 8 * 8)
    out_height = max(64, int(height * scale) // 8 * 8)
    return out_width, out_height


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--repo", required=True, type=Path)
    parser.add_argument("--video", required=True, type=Path)
    parser.add_argument("--mask", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--runtime-dir", required=True, type=Path)
    parser.add_argument("--max-dimension", type=int, default=432)
    parser.add_argument("--subvideo-length", type=int, default=50)
    args = parser.parse_args()

    repo = args.repo.resolve()
    video = args.video.resolve()
    mask = args.mask.resolve()
    output = args.output.resolve()
    runtime_dir = args.runtime_dir.resolve()
    inference = repo / "inference_propainter.py"
    if not inference.is_file():
        raise RuntimeError("Configured ProPainterPath does not contain inference_propainter.py.")
    if not video.is_file() or not mask.is_dir():
        raise RuntimeError("Video or generated mask folder is missing.")
    if not 256 <= args.max_dimension <= 1280 or not 20 <= args.subvideo_length <= 100:
        raise RuntimeError("ProPainter memory settings are outside the supported safe range.")

    mask_files = sorted(mask.glob("*.png"))
    if not mask_files:
        raise RuntimeError("No frame masks were generated.")
    runtime_dir.mkdir(parents=True, exist_ok=True)
    output.mkdir(parents=True, exist_ok=True)
    matplotlib_cache = runtime_dir / "matplotlib"
    matplotlib_cache.mkdir(parents=True, exist_ok=True)
    expected_checkpoints = ("raft-things.pth", "recurrent_flow_completion.pth", "ProPainter.pth")
    runtime_weights = runtime_dir / "weights"
    repo_weights = repo / "weights"
    if all((runtime_weights / name).is_file() for name in expected_checkpoints):
        working_dir = runtime_dir
    elif all((repo_weights / name).is_file() for name in expected_checkpoints):
        working_dir = repo
    else:
        missing = [name for name in expected_checkpoints
                   if not (runtime_weights / name).is_file() and not (repo_weights / name).is_file()]
        raise RuntimeError("Missing ProPainter checkpoints: " + ", ".join(missing))
    width, height = dimensions(video, args.max_dimension)

    # ProPainter resolves its checkpoint directory from the working directory.
    # Verify weights first so an inference request never silently downloads GBs.
    try:
        import torch
        has_cuda = torch.cuda.is_available()
    except Exception as error:
        raise RuntimeError(f"PyTorch could not be loaded by {sys.executable}: {error}") from error

    compatibility_entry = Path(__file__).resolve().with_name("propainter_compat.py")
    command = [sys.executable, str(compatibility_entry), "--inference-script", str(inference),
               "--video", str(video), "--mask", str(mask),
               "--output", str(output), "--width", str(width), "--height", str(height),
               "--subvideo_length", str(args.subvideo_length), "--mask_dilation", "0"]
    if has_cuda:
        command.append("--fp16")
    print(json.dumps({"stage": "AIProcessing", "width": width, "height": height,
                      "frames": len(mask_files), "device": "cuda" if has_cuda else "cpu"}), flush=True)
    environment = os.environ.copy()
    environment["MPLCONFIGDIR"] = str(matplotlib_cache)
    environment["PYTHONDONTWRITEBYTECODE"] = "1"
    result = subprocess.run(command, cwd=str(working_dir), env=environment, check=False)
    if result.returncode != 0:
        raise RuntimeError(f"ProPainter exited with code {result.returncode}.")
    result_video = output / video.stem / "inpaint_out.mp4"
    if not result_video.is_file() or result_video.stat().st_size == 0:
        raise RuntimeError("ProPainter completed without creating inpaint_out.mp4.")
    print(json.dumps({"output": str(result_video)}))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(json.dumps({"error": str(error)}), file=sys.stderr, flush=True)
        raise SystemExit(1)
