#!/usr/bin/env python3
"""Validate/export Felling scene geometry without Unity or raster edits.

The report verifies the declared layout, not the art's true collision edges.
Collision and occlusion polygons remain proposals until tested in Unity.
"""

from __future__ import annotations

import argparse
from collections import deque
import copy
import hashlib
import json
import math
from pathlib import Path, PurePosixPath
import sys


class LayoutError(ValueError):
    """A scene manifest is invalid or its required asset is unavailable."""


def _require(condition, message):
    if not condition:
        raise LayoutError(message)


def _number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def _integer(value):
    return isinstance(value, int) and not isinstance(value, bool)


def _pair(value, label, integers=False):
    predicate = _integer if integers else _number
    _require(isinstance(value, (list, tuple)) and len(value) == 2 and all(predicate(x) for x in value),
             f"{label} must contain two {'integer' if integers else 'finite numeric'} coordinates")
    return tuple(value)


def _object(value, label):
    _require(isinstance(value, dict), f"{label} must be an object")
    return value


def _list(value, label):
    _require(isinstance(value, list), f"{label} must be an array")
    return value


def image_to_cell(point, grid):
    """Map top-left-origin art pixels to the game grid; overhang may be negative."""
    x, y = point
    ox, oy = grid["imageGroundOrigin"]
    gx, gy = grid["regionOrigin"]
    size = grid["artPixelsPerCell"]
    return math.floor((x - ox) / size) + gx, math.floor((y - oy) / size) + gy


def cell_to_image(cell, grid):
    """Return a cell center in reference-image pixels, never its upper-left edge."""
    x, y = cell
    ox, oy = grid["imageGroundOrigin"]
    gx, gy = grid["regionOrigin"]
    size = grid["artPixelsPerCell"]
    return ox + (x - gx + 0.5) * size, oy + (y - gy + 0.5) * size


def cell_in_region(cell, grid):
    x, y = cell
    gx, gy = grid["regionOrigin"]
    width, height = grid["regionSize"]
    return (0 <= x < grid["zoneWidth"] and 0 <= y < grid["zoneHeight"]
            and gx <= x < gx + width and gy <= y < gy + height)


def _cross(a, b, c):
    return (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0])


def _on_segment(point, a, b):
    return (abs(_cross(a, b, point)) <= 1e-8
            and min(a[0], b[0]) - 1e-8 <= point[0] <= max(a[0], b[0]) + 1e-8
            and min(a[1], b[1]) - 1e-8 <= point[1] <= max(a[1], b[1]) + 1e-8)


def _segments_intersect(a, b, c, d):
    ac, ad, ca, cb = _cross(a, b, c), _cross(a, b, d), _cross(c, d, a), _cross(c, d, b)
    if ((ac > 0 > ad or ad > 0 > ac) and (ca > 0 > cb or cb > 0 > ca)):
        return True
    return any((_on_segment(c, a, b), _on_segment(d, a, b),
                _on_segment(a, c, d), _on_segment(b, c, d)))


def validate_polygon(polygon, label):
    _list(polygon, label)
    _require(len(polygon) >= 3, f"{label} polygon needs at least three vertices")
    points = [_pair(p, f"{label} polygon vertex") for p in polygon]
    # A repeated closing vertex is accepted; a duplicate within an edge is not.
    if points[0] == points[-1]:
        points.pop()
    _require(len(set(points)) >= 3, f"{label} polygon needs three distinct vertices")
    edges = list(zip(points, points[1:] + points[:1]))
    _require(all(a != b for a, b in edges), f"{label} polygon has a zero-length edge")
    area = abs(sum(a[0] * b[1] - b[0] * a[1] for a, b in edges)) / 2
    _require(math.isfinite(area) and area > 1e-8, f"{label} polygon has no finite positive area")
    for i, (a, b) in enumerate(edges):
        for j in range(i + 1, len(edges)):
            if j == i + 1 or (i == 0 and j == len(edges) - 1):
                continue
            _require(not _segments_intersect(a, b, *edges[j]), f"{label} polygon self-intersects")
    return {"vertexCount": len(points), "areaPixelsSquared": area}


def point_in_polygon(point, polygon):
    """Boundary-inclusive ray casting. Polygon validity is checked separately."""
    inside = False
    x, y = point
    for i, a in enumerate(polygon):
        b = polygon[(i + 1) % len(polygon)]
        if _on_segment(point, a, b):
            return True
        if (a[1] > y) != (b[1] > y):
            crossing_x = (b[0] - a[0]) * (y - a[1]) / (b[1] - a[1]) + a[0]
            if x < crossing_x:
                inside = not inside
    return inside


def reachable_cells(start, walkable):
    """Return BFS predecessors; diagonals require both adjacent orthogonal cells."""
    start = tuple(start)
    if start not in walkable:
        return {}
    previous = {start: None}
    frontier = deque([start])
    while frontier:
        x, y = frontier.popleft()
        for dx, dy in ((0, -1), (-1, 0), (1, 0), (0, 1), (-1, -1), (1, -1), (-1, 1), (1, 1)):
            target = (x + dx, y + dy)
            if target not in walkable or target in previous:
                continue
            if dx and dy and ((x + dx, y) not in walkable or (x, y + dy) not in walkable):
                continue
            previous[target] = (x, y)
            frontier.append(target)
    return previous


def _safe_path(value, base_dir):
    _require(isinstance(value, str) and value and "\x00" not in value
             and "\\" not in value and ":" not in value, "asset path must be a safe relative POSIX path")
    path = PurePosixPath(value)
    _require(not path.is_absolute() and ".." not in path.parts and path.parts,
             f"asset path escapes its directory: {value!r}")
    base = Path(base_dir).resolve()
    resolved = (base / str(path)).resolve()
    _require(resolved.is_relative_to(base), f"asset path resolves outside its directory: {value!r}")
    return resolved


def _file_hash(path):
    with Path(path).open("rb") as source:
        return hashlib.file_digest(source, "sha256").hexdigest()


def _canonical_layout_hash(layout):
    try:
        canonical = json.dumps(layout, sort_keys=True, separators=(",", ":"), allow_nan=False).encode("utf-8")
    except (TypeError, ValueError) as error:
        raise LayoutError(f"layout must contain finite, JSON-compatible values: {error}") from error
    return hashlib.sha256(canonical).hexdigest()


def _asset_status(path, base_dir, required=False):
    resolved = _safe_path(path, base_dir)
    present = resolved.is_file()
    _require(not required or present, f"required asset is missing: {path}")
    result = {"path": path, "exists": present}
    if present:
        result["sha256"] = _file_hash(resolved)
    if present and resolved.suffix.lower() == ".png":
        # Read-only inspection: verify the container, then decode every pixel. No
        # image is drawn, edited, resaved, or exported by this tool.
        try:
            from PIL import Image
        except ImportError as error:
            raise LayoutError("Pillow is required for read-only PNG decode and alpha validation") from error
        try:
            with Image.open(resolved) as image:
                _require(image.format == "PNG", f"asset does not contain PNG data: {path}")
                image.verify()
            with Image.open(resolved) as image:
                image.load()
                result.update(width=image.width, height=image.height, mode=image.mode, decoded=True)
                # Conversion here only exposes alpha for palette/tRNS PNGs; no
                # converted pixels are written or used as generated artwork.
                alpha = image.getchannel("A") if "A" in image.getbands() else image.convert("RGBA").getchannel("A")
                histogram = alpha.histogram()
                result.update(alphaExtrema=list(alpha.getextrema()),
                              transparentPixelCount=histogram[0], opaquePixelCount=histogram[255])
        except (OSError, ValueError, SyntaxError) as error:
            raise LayoutError(f"could not fully decode PNG asset {path}: {error}") from error
    return result


def _validate_asset_metadata(item, metadata, reference):
    identifier = item["id"]
    if "requireTransparency" in item:
        _require(isinstance(item["requireTransparency"], bool),
                 f"asset {identifier} requireTransparency must be boolean")
    if metadata["exists"] and item.get("requireTransparency"):
        _require(metadata.get("transparentPixelCount", 0) > 0 and metadata.get("opaquePixelCount", 0) > 0,
                 f"asset {identifier} requires both fully transparent and fully opaque pixels")
    if metadata["exists"] and (item["role"] in ("background", "clean-plate") or identifier == "clean-plate"):
        _require((metadata.get("width"), metadata.get("height")) == (reference["width"], reference["height"]),
                 f"asset {identifier} dimensions must match reference dimensions")
    frame_keys = ("frameWidth", "frameHeight", "columns", "rows")
    if any(key in item for key in frame_keys):
        _require(all(_integer(item.get(key)) and item[key] > 0 for key in frame_keys),
                 f"asset {identifier} frame geometry requires positive integer frameWidth, frameHeight, columns and rows")
        if metadata["exists"]:
            _require((metadata.get("width"), metadata.get("height"))
                     == (item["frameWidth"] * item["columns"], item["frameHeight"] * item["rows"]),
                     f"asset {identifier} frame geometry does not exactly cover the decoded sheet dimensions")


def _identified_items(layout, key):
    items = _list(layout.get(key), key)
    identifiers = set()
    for item in items:
        _object(item, f"{key} entry")
        identifier = item.get("id")
        _require(isinstance(identifier, str) and identifier.strip(), f"{key} entry requires a nonempty id")
        _require(identifier not in identifiers, f"{key} has duplicate id: {identifier}")
        identifiers.add(identifier)
    return items


def _validate_grid(layout):
    reference = _object(layout.get("reference"), "reference")
    for key in ("width", "height"):
        _require(_integer(reference.get(key)) and reference[key] > 0, f"reference.{key} must be a positive integer")
    grid = _object(layout.get("grid"), "grid")
    for key in ("zoneWidth", "zoneHeight", "artPixelsPerCell"):
        _require(_integer(grid.get(key)) and grid[key] > 0, f"grid.{key} must be a positive integer")
    gx, gy = _pair(grid.get("regionOrigin"), "grid.regionOrigin", integers=True)
    width, height = _pair(grid.get("regionSize"), "grid.regionSize", integers=True)
    ox, oy = _pair(grid.get("imageGroundOrigin"), "grid.imageGroundOrigin")
    _require(gx >= 0 and gy >= 0 and width > 0 and height > 0
             and gx + width <= grid["zoneWidth"] and gy + height <= grid["zoneHeight"],
             "art region must have positive size and fit inside the zone")
    _require(width * height <= 1_000_000, "art region exceeds one million cells")
    size = grid["artPixelsPerCell"]
    _require(ox >= 0 and oy >= 0 and ox + width * size <= reference["width"]
             and oy + height * size <= reference["height"], "playable art region must fit inside the reference image")
    return grid


def occupancy(layout):
    grid = layout["grid"]
    gx, gy = grid["regionOrigin"]
    width, height = grid["regionSize"]
    walkable = set()
    for y in range(gy, gy + height):
        for x in range(gx, gx + width):
            center = cell_to_image((x, y), grid)
            if (point_in_polygon(center, layout["walkablePolygon"])
                    and not any(point_in_polygon(center, blocker["polygon"]) for blocker in layout["blockers"])):
                walkable.add((x, y))
    return walkable


def validate_layout(layout, base_dir=None, require_assets=False):
    """Return JSON-safe structural findings, or raise LayoutError on any failure."""
    _object(layout, "layout")
    _require(layout.get("schemaVersion") == 1, "schemaVersion must be 1")
    _require(layout.get("id") == "felling-site", "layout.id must be felling-site")
    grid = _validate_grid(layout)
    geometry = [{"id": "walkable", **validate_polygon(layout.get("walkablePolygon"), "walkablePolygon")}]
    for key in ("blockers", "occluders", "effects"):
        for item in _identified_items(layout, key):
            geometry.append({"id": item["id"], "collection": key,
                             **validate_polygon(item.get("polygon"), f"{key}.{item['id']}")})
            if key == "blockers":
                _require(item.get("kind") in ("root", "water", "cliff"), f"blocker {item['id']} has unknown kind")
                _require(isinstance(item.get("blocksSight"), bool), f"blocker {item['id']} requires boolean blocksSight")
            elif key == "occluders":
                _require(_number(item.get("sortFootY")), f"occluder {item['id']} requires finite sortFootY")
                if "ownerId" in item:
                    _require(isinstance(item["ownerId"], str)
                             and item["ownerId"] in {blocker["id"] for blocker in layout["blockers"]},
                             f"occluder {item['id']} ownerId must reference an existing blocker")
            else:
                _require(isinstance(item.get("kind"), str) and item["kind"], f"effect {item['id']} requires kind")
    strikes = _identified_items(layout, "sterilePositions")
    _require(len(strikes) == 6, "sterilePositions must contain exactly six strikes")
    _object(layout.get("seventh"), "seventh")
    markers = [("spawn", layout.get("spawn")), ("entrance", layout.get("entrance"))]
    markers += [(item["id"], item.get("cell")) for item in strikes]
    markers.append(("seventh", layout["seventh"].get("cell")))
    cells = {}
    for identifier, value in markers:
        cell = _pair(value, identifier, integers=True)
        _require(0 <= cell[0] < grid["zoneWidth"] and 0 <= cell[1] < grid["zoneHeight"],
                 f"{identifier} cell is outside the zone")
        _require(cell_in_region(cell, grid), f"{identifier} cell is outside the art region")
        _require(identifier not in cells, f"marker has duplicate id: {identifier}")
        cells[identifier] = cell
    strike_cells = [tuple(item["cell"]) for item in strikes]
    distinct = strike_cells + [cells["spawn"], cells["seventh"]]
    _require(len(set(distinct)) == 8, "six strike cells, seventh and spawn must all be distinct")
    walkable = occupancy(layout)
    for identifier, cell in cells.items():
        _require(cell in walkable, f"{identifier} cell center is blocked or outside the walkable polygon")
    previous = reachable_cells(cells["entrance"], walkable)
    routes = []
    for identifier, cell in cells.items():
        if identifier == "entrance":
            continue
        _require(cell in previous, f"{identifier} is unreachable from entrance without diagonal corner cutting")
        route = []
        cursor = cell
        while cursor is not None:
            route.append(list(cursor))
            cursor = previous[cursor]
        routes.append({"target": identifier, "reachable": True, "steps": len(route) - 1,
                       "cells": list(reversed(route))})
    base_dir = Path(base_dir or ".")
    reference = _asset_status(layout["reference"].get("path"), base_dir, require_assets)
    if "width" in reference:
        _require((reference["width"], reference["height"]) == (layout["reference"]["width"], layout["reference"]["height"]),
                 "reference PNG dimensions do not match the layout")
    assets = []
    for item in _identified_items(layout, "assets"):
        _require(item.get("status") in ("reference", "generated", "pending"), f"asset {item['id']} has invalid status")
        _require(isinstance(item.get("role"), str) and item["role"], f"asset {item['id']} requires role")
        _require(not require_assets or item["status"] != "pending", f"required asset is still pending: {item['id']}")
        metadata = _asset_status(item.get("path"), base_dir, require_assets)
        _validate_asset_metadata(item, metadata, layout["reference"])
        assets.append({"id": item["id"], "status": item["status"], **metadata})
    provenance = {
        "sourceLayoutCanonicalSha256": _canonical_layout_hash(layout),
        "canonicalization": "UTF-8 JSON; keys sorted; compact separators; nonfinite values rejected",
        "assets": [{"path": path, **value} for path, value in
                   {item["path"]: {"exists": item["exists"], "sha256": item.get("sha256")}
                    for item in [reference] + assets}.items()],
    }
    return {
        "schemaVersion": 1, "id": layout["id"], "structuralValidation": "passed",
        "collisionGeometryStatus": "proposed; requires Unity visual and gameplay QA",
        "artApprovalStatus": "not evaluated by structural validation",
        "coordinateConvention": "image origin top-left; zone cell y increases downward; cell-center sampling",
        "diagonalCornerCutting": False, "walkableCellCount": len(walkable),
        "blockedCellCount": grid["regionSize"][0] * grid["regionSize"][1] - len(walkable),
        "reachableCellCount": len(previous), "routes": routes, "geometry": geometry,
        "reference": reference, "assets": assets,
        "provenance": provenance,
        "limitations": ["Cell-center occupancy does not prove actor-radius clearance.",
                        "Pixels above imageGroundOrigin are visual overhang, never playable cells.",
                        "Occluder polygons do not block movement; blockers do.",
                        "Runtime sorting, fog of war, effects and visual similarity require Unity QA."],
    }


def load_layout(path):
    try:
        return json.loads(Path(path).read_text(encoding="utf-8"))
    except (OSError, ValueError) as error:
        raise LayoutError(f"could not read layout {path}: {error}") from error


def export_layout(layout, out_dir, base_dir=None, require_assets=False, source_layout_path=None):
    report = validate_layout(layout, base_dir, require_assets)
    if source_layout_path is not None:
        report["provenance"].update(sourceLayoutFilename=Path(source_layout_path).name,
                                     sourceLayoutFileSha256=_file_hash(source_layout_path))
    walkable = occupancy(layout)
    grid = layout["grid"]
    gx, gy = grid["regionOrigin"]
    width, height = grid["regionSize"]
    occupied = {"schemaVersion": 1, "id": layout["id"], "regionOrigin": [gx, gy],
                "regionSize": [width, height], "rowOrder": "increasing zone y (image downward)",
                "legend": {"0": "blocked", "1": "walkable"},
                "provenance": report["provenance"],
                "rows": [[int((x, y) in walkable) for x in range(gx, gx + width)]
                         for y in range(gy, gy + height)]}

    def marker(cell):
        return {"cell": list(cell), "imageCenter": list(cell_to_image(cell, grid))}

    # Start with the authored manifest to preserve camera/policies, source pixel
    # centers, radius hints and future optional handoff data. Augment markers.
    unity = {**copy.deepcopy(layout),
             "status": "prepared offline; not imported or verified in Unity",
             "coordinateConvention": report["coordinateConvention"],
             "worldTransform": "Adapter must use the game's grid-to-world transform; image y is downward.",
             "spawn": marker(layout["spawn"]), "entrance": marker(layout["entrance"]),
             "sterilePositions": [{**item, **marker(item["cell"])} for item in layout["sterilePositions"]],
             "seventh": {**layout["seventh"], **marker(layout["seventh"]["cell"])},
             "walkablePolygon": layout["walkablePolygon"],
             "blockers": layout["blockers"], "occluders": layout["occluders"],
             "effects": layout["effects"], "assets": layout["assets"],
             "provenance": report["provenance"],
             "assetPathBase": "source layout directory; paths are not relative to this export directory"}
    destination = Path(out_dir)
    destination.mkdir(parents=True, exist_ok=True)
    for name, contents in (("occupancy.json", occupied), ("geometry-report.json", report), ("unity-layout.json", unity)):
        (destination / name).write_text(json.dumps(contents, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    return report


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    for name in ("validate", "export"):
        command = commands.add_parser(name)
        command.add_argument("layout", type=Path)
        command.add_argument("--require-assets", action="store_true", help="fail on missing or pending assets")
        if name == "export":
            command.add_argument("--out", type=Path, required=True)
    args = parser.parse_args(argv)
    try:
        layout = load_layout(args.layout)
        if args.command == "export":
            report = export_layout(layout, args.out, args.layout.parent, args.require_assets, args.layout)
        else:
            report = validate_layout(layout, args.layout.parent, args.require_assets)
        print(json.dumps({"structuralValidation": report["structuralValidation"],
                          "walkableCellCount": report["walkableCellCount"],
                          "routesVerified": len(report["routes"]),
                          "collisionGeometryStatus": report["collisionGeometryStatus"]}, indent=2))
        return 0
    except (LayoutError, OSError) as error:
        print(f"ERROR: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
