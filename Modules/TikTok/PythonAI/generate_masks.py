#!/usr/bin/env python3
"""Create frame-sized binary masks for one manually selected, authorized region."""
import argparse
import json
import math
from pathlib import Path

import cv2
import numpy as np


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--video", required=True, type=Path)
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--x", required=True, type=float, help="Normalized left coordinate (0..1).")
    parser.add_argument("--y", required=True, type=float, help="Normalized top coordinate (0..1).")
    parser.add_argument("--width", required=True, type=float, help="Normalized region width (0..1).")
    parser.add_argument("--height", required=True, type=float, help="Normalized region height (0..1).")
    parser.add_argument("--dilation", type=int, default=6, help="Mask expansion radius in pixels.")
    args = parser.parse_args()

    values = (args.x, args.y, args.width, args.height)
    if not all(math.isfinite(value) for value in values):
        parser.error("Region coordinates must be finite numbers.")
    if args.x < 0 or args.y < 0 or args.width <= 0 or args.height <= 0 or args.x + args.width > 1 or args.y + args.height > 1:
        parser.error("Region must be inside the video frame and have a non-zero size.")
    if not 0 <= args.dilation <= 32:
        parser.error("Dilation must be between 0 and 32 pixels.")

    capture = cv2.VideoCapture(str(args.video))
    if not capture.isOpened():
        raise RuntimeError("OpenCV could not open the downloaded video.")
    width = int(capture.get(cv2.CAP_PROP_FRAME_WIDTH))
    height = int(capture.get(cv2.CAP_PROP_FRAME_HEIGHT))
    expected_frames = int(capture.get(cv2.CAP_PROP_FRAME_COUNT))
    if width <= 0 or height <= 0 or expected_frames <= 0:
        capture.release()
        raise RuntimeError("The video has invalid frame dimensions or frame count.")

    x1 = max(0, min(width - 1, int(math.floor(args.x * width))))
    y1 = max(0, min(height - 1, int(math.floor(args.y * height))))
    x2 = max(x1 + 1, min(width, int(math.ceil((args.x + args.width) * width))))
    y2 = max(y1 + 1, min(height, int(math.ceil((args.y + args.height) * height))))
    base_mask = np.zeros((height, width), dtype=np.uint8)
    base_mask[y1:y2, x1:x2] = 255
    if args.dilation:
        diameter = args.dilation * 2 + 1
        kernel = cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (diameter, diameter))
        base_mask = cv2.dilate(base_mask, kernel, iterations=1)

    args.output.mkdir(parents=True, exist_ok=True)
    count = 0
    while True:
        ok, frame = capture.read()
        if not ok:
            break
        if frame.shape[1] != width or frame.shape[0] != height:
            capture.release()
            raise RuntimeError("Video frame dimensions changed during mask generation.")
        mask_path = args.output / f"{count:06d}.png"
        if not cv2.imwrite(str(mask_path), base_mask):
            capture.release()
            raise RuntimeError(f"Could not write mask frame {count}.")
        count += 1
    capture.release()
    if count == 0 or count != expected_frames:
        raise RuntimeError(f"Mask count mismatch: generated {count}, video reports {expected_frames} frames.")

    print(json.dumps({"width": width, "height": height, "frames": count, "masks": count}))
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as error:  # surfaced as a concise worker error
        print(json.dumps({"error": str(error)}))
        raise SystemExit(1)
