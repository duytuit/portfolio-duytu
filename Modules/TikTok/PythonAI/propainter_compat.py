#!/usr/bin/env python3
"""Compatibility entry point for ProPainter with newer torchvision builds."""
import argparse
import runpy
import sys
from pathlib import Path

import cv2
import numpy as np
import torch
import torchvision


def read_video_with_opencv(filename, *args, **kwargs):
    capture = cv2.VideoCapture(str(filename))
    if not capture.isOpened():
        raise RuntimeError(f"OpenCV could not read video: {filename}")

    fps = float(capture.get(cv2.CAP_PROP_FPS))
    frames = []
    while True:
        ok, frame = capture.read()
        if not ok:
            break
        frames.append(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB))
    capture.release()
    if not frames:
        raise RuntimeError(f"Video contains no readable frames: {filename}")

    video = torch.from_numpy(np.stack(frames, axis=0))
    output_format = kwargs.get("output_format", "THWC")
    if output_format == "TCHW":
        video = video.permute(0, 3, 1, 2).contiguous()
    return video, None, {"video_fps": fps if fps > 0 else 24.0}


def main() -> int:
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("--inference-script", required=True, type=Path)
    options, inference_arguments = parser.parse_known_args()
    inference_script = options.inference_script.resolve()
    if not inference_script.is_file():
        raise RuntimeError("Configured ProPainter inference script was not found.")

    # New torchvision releases removed read_video; ProPainter only needs the
    # RGB frames and FPS from its result. Its CLI does not use decoded audio.
    torchvision.io.read_video = read_video_with_opencv
    sys.path.insert(0, str(inference_script.parent))
    sys.argv = [str(inference_script), *inference_arguments]
    runpy.run_path(str(inference_script), run_name="__main__")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:
        print(f"ProPainter compatibility runner: {error}", file=sys.stderr, flush=True)
        raise
