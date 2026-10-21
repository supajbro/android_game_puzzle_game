#!/usr/bin/env python3
"""Clean and export the five Jelly Riot 10x4 animation sheets.

The source artwork is never overwritten. Grid boundaries are calculated proportionally so
non-divisible source dimensions are handled without losing edge pixels. Cleanup operates only on
alpha connectivity/geometry; it never keys colours, so highlights, faces and jelly hues survive.
"""

from __future__ import annotations

import argparse
import json
from collections import deque
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter


COLOURS = ("Coral", "Lemon", "Aqua", "Lilac", "Mint")
CATEGORIES = ("Idle", "Blink", "Landing", "Recovery")
GRID_COLUMNS = 10
GRID_ROWS = 4
FRAME_SIZE = 256


def proportional_edges(length: int, divisions: int) -> list[int]:
    """Return inclusive grid boundaries spanning every source pixel exactly once."""
    return [round(index * length / divisions) for index in range(divisions + 1)]


def largest_run(values: list[int]) -> tuple[int, int] | None:
    if not values:
        return None
    best = current_start = previous = values[0]
    best_start = best_end = values[0]
    for value in values[1:]:
        if value != previous + 1:
            if previous - current_start > best_end - best_start:
                best_start, best_end = current_start, previous
            current_start = value
        previous = value
    if previous - current_start > best_end - best_start:
        best_start, best_end = current_start, previous
    return best_start, best_end


def central_component(mask: Image.Image) -> Image.Image:
    """Keep the largest substantial connected component after a small morphological opening."""
    width, height = mask.size
    opened = mask.filter(ImageFilter.MinFilter(5)).filter(ImageFilter.MaxFilter(5))
    pixels = opened.load()
    visited = bytearray(width * height)
    components: list[list[tuple[int, int]]] = []
    for y in range(height):
        for x in range(width):
            flat = y * width + x
            if visited[flat] or pixels[x, y] == 0:
                continue
            visited[flat] = 1
            queue = deque([(x, y)])
            component: list[tuple[int, int]] = []
            while queue:
                current_x, current_y = queue.popleft()
                component.append((current_x, current_y))
                for next_x, next_y in ((current_x - 1, current_y), (current_x + 1, current_y),
                                       (current_x, current_y - 1), (current_x, current_y + 1)):
                    if next_x < 0 or next_x >= width or next_y < 0 or next_y >= height:
                        continue
                    next_flat = next_y * width + next_x
                    if not visited[next_flat] and pixels[next_x, next_y] != 0:
                        visited[next_flat] = 1
                        queue.append((next_x, next_y))
            components.append(component)

    if not components:
        return Image.new("L", mask.size, 0)

    centre_x, centre_y = width / 2, height / 2
    components.sort(key=lambda points: (
        any(abs(x - centre_x) < width * 0.15 and abs(y - centre_y) < height * 0.2
            for x, y in points), len(points)), reverse=True)
    result = Image.new("L", mask.size, 0)
    result_pixels = result.load()
    for x, y in components[0]:
        result_pixels[x, y] = 255
    return result.filter(ImageFilter.MaxFilter(9))


def clean_cell(cell: Image.Image) -> tuple[Image.Image, dict[str, object]]:
    rgba = cell.convert("RGBA")
    alpha = rgba.getchannel("A")
    binary = alpha.point(lambda value: 255 if value >= 12 else 0)
    width, height = rgba.size
    mask_pixels = binary.load()
    dense_rows = [y for y in range(height)
                  if sum(mask_pixels[x, y] != 0 for x in range(width)) >= max(8, round(width * 0.22))]
    dense_columns = [x for x in range(width)
                     if sum(mask_pixels[x, y] != 0 for y in range(height)) >= max(8, round(height * 0.18))]
    row_run = largest_run(dense_rows)
    column_run = largest_run(dense_columns)
    if row_run is None or column_run is None:
        return rgba, {"fallback": True, "reason": "no dense central body detected"}

    padding = max(4, round(min(width, height) * 0.025))
    left = max(0, column_run[0] - padding)
    right = min(width, column_run[1] + padding + 1)
    top = max(0, row_run[0] - padding)
    bottom = min(height, row_run[1] + padding + 1)
    geometry = Image.new("L", rgba.size, 0)
    ImageDraw.Draw(geometry).rectangle((left, top, right - 1, bottom - 1), fill=255)
    keep = central_component(Image.composite(binary, Image.new("L", rgba.size, 0), geometry))
    cleaned_alpha = Image.composite(alpha, Image.new("L", rgba.size, 0), keep)
    cleaned = rgba.copy()
    cleaned.putalpha(cleaned_alpha)
    bbox = cleaned_alpha.getbbox()
    if bbox is None or (bbox[2] - bbox[0]) * (bbox[3] - bbox[1]) < width * height * 0.08:
        return rgba, {"fallback": True, "reason": "cleanup rejected as over-aggressive"}
    return cleaned, {"fallback": False, "body_bounds": list(bbox)}


def checkerboard(size: tuple[int, int], square: int = 16) -> Image.Image:
    image = Image.new("RGBA", size, (48, 48, 65, 255))
    draw = ImageDraw.Draw(image)
    for y in range(0, size[1], square):
        for x in range(0, size[0], square):
            if (x // square + y // square) % 2:
                draw.rectangle((x, y, x + square - 1, y + square - 1), fill=(74, 74, 94, 255))
    return image


def locate_source(source_root: Path, colour: str) -> Path:
    candidates = (
        source_root / f"Jelly_{colour}_Sheet.png",
        source_root / f"Jelly_{colour}_animations.png",
        source_root / f"Jelly_{colour}_animation.png",
    )
    for candidate in candidates:
        if candidate.exists():
            return candidate
    matches = sorted(source_root.glob(f"Jelly_{colour}_*.png"))
    if len(matches) == 1:
        return matches[0]
    raise FileNotFoundError(f"Could not uniquely locate the {colour} animation sheet in {source_root}")


def process_sheet(source: Path, colour: str, frame_root: Path, preview_root: Path) -> dict[str, object]:
    sheet = Image.open(source).convert("RGBA")
    x_edges = proportional_edges(sheet.width, GRID_COLUMNS)
    y_edges = proportional_edges(sheet.height, GRID_ROWS)
    preview = checkerboard((GRID_COLUMNS * FRAME_SIZE, GRID_ROWS * FRAME_SIZE))
    records = []
    for row, category in enumerate(CATEGORIES):
        category_root = frame_root / colour / category
        category_root.mkdir(parents=True, exist_ok=True)
        for column in range(GRID_COLUMNS):
            crop_box = (x_edges[column], y_edges[row], x_edges[column + 1], y_edges[row + 1])
            cleaned, cleanup = clean_cell(sheet.crop(crop_box))
            bounds = cleaned.getchannel("A").getbbox()
            if bounds is None:
                raise RuntimeError(f"Cleanup removed {colour} {category} frame {column}")
            body = cleaned.crop(bounds)
            scale = min(236 / body.width, 236 / body.height)
            body = body.resize((round(body.width * scale), round(body.height * scale)), Image.Resampling.LANCZOS)
            frame = Image.new("RGBA", (FRAME_SIZE, FRAME_SIZE), (0, 0, 0, 0))
            frame.alpha_composite(body, ((FRAME_SIZE - body.width) // 2, 10))
            output = category_root / f"Jelly_{colour}_{category}_{column:02d}.png"
            frame.save(output, format="PNG", optimize=True)
            preview.alpha_composite(frame, (column * FRAME_SIZE, row * FRAME_SIZE))
            records.append({
                "category": category,
                "frame": column,
                "crop": list(crop_box),
                "output": output.as_posix(),
                **cleanup,
            })

    preview_root.mkdir(parents=True, exist_ok=True)
    preview_path = preview_root / f"Jelly_{colour}_ContactSheet.png"
    preview.save(preview_path, format="PNG", optimize=True)
    return {
        "colour": colour,
        "source": source.as_posix(),
        "source_size": list(sheet.size),
        "preview": preview_path.as_posix(),
        "frames": records,
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", type=Path, default=Path(__file__).resolve().parents[2])
    args = parser.parse_args()
    project_root = args.project_root.resolve()
    source_root = project_root / "Assets/_Project/Art/Animations"
    frame_root = source_root / "Frames40"
    preview_root = project_root / "Tools/Art/Previews"
    reports = []
    for colour in COLOURS:
        reports.append(process_sheet(locate_source(source_root, colour), colour, frame_root, preview_root))

    report_path = project_root / "Tools/Art/animation_cleanup_report.json"
    report_path.write_text(json.dumps({"frame_size": [FRAME_SIZE, FRAME_SIZE], "sheets": reports}, indent=2),
                           encoding="utf-8")
    exported = sum(len(sheet["frames"]) for sheet in reports)
    if exported != 200:
        raise RuntimeError(f"Expected 200 frames, exported {exported}")
    print(f"Exported {exported} transparent RGBA frames and {len(reports)} contact sheets.")
    print(f"Report: {report_path}")


if __name__ == "__main__":
    main()
