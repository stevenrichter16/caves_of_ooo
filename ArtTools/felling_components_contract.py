"""Validate the offline component manifest and immutable removal state.

No rendering or raster mutation occurs here. Strict asset checks read PNG bytes with
Pillow; ordinary structural checks and state transitions need only the standard library.
Passing these checks does not prove silhouette quality, hidden repair art, or Unity behavior.
"""
from collections.abc import Iterable, Mapping
import hashlib
import io
import math
from pathlib import Path, PurePosixPath
import re
from urllib.parse import unquote


class ManifestValidationError(ValueError):
    """An entire requested state transition was rejected without changing its inputs."""


def _text(value):
    return isinstance(value, str) and bool(value.strip())


def _number(value):
    # bool is an int subclass; enormous Python ints are finite without float conversion.
    return type(value) is int or (type(value) is float and math.isfinite(value))


def _vector(value, length, integers=False):
    return isinstance(value, list) and len(value) == length and all(
        type(item) is int if integers else _number(item) for item in value
    )


def validate_manifest(manifest, base_dir=None, require_assets=False):
    """Return a JSON-safe report; malformed manifests produce errors, not partial reads.

    Paths use portable, normalized POSIX-relative syntax. When base_dir is supplied,
    symlink resolution must remain within that directory even without asset reads.
    report.path is an output destination, so strict validation never requires it to exist.
    """
    errors, warnings, jobs = [], [], []
    counts = {"componentCount": 0, "mutableCount": 0, "contributingCount": 0}
    root = None
    if type(require_assets) is not bool:
        errors.append("require_assets must be a boolean")
        require_assets = False
    if base_dir is not None:
        try:
            root = Path(base_dir).resolve()
            if require_assets and not root.is_dir():
                errors.append("base_dir must be an existing directory for strict asset checks")
        except (TypeError, ValueError, OSError, RuntimeError) as exc:
            errors.append(f"base_dir is invalid: {exc}")
    if require_assets and root is None:
        errors.append("base_dir is required for strict asset checks")

    def error(label, message):
        errors.append(f"{label}: {message}")

    def object_at(value, label):
        if not isinstance(value, dict):
            error(label, "must be an object")
            return None
        return value

    def text_at(value, label):
        if not _text(value):
            error(label, "must be a nonempty string")
            return False
        return True

    def flag(value, label):
        if type(value) is not bool:
            error(label, "must be a boolean")

    def safe_path(value, label):
        if not text_at(value, label):
            return None
        parts = value.split("/")
        if (value != value.strip() or any(char in value for char in ("\\", "\x00", ":"))
                or PurePosixPath(value).is_absolute()
                or any(part in ("", ".", "..") for part in parts)
                or unquote(value) != value):
            error(label, "must be a normalized, unencoded, confined POSIX-relative path")
            return None
        if root is None:
            return value
        try:
            path = (root / value).resolve()
            path.relative_to(root)
            return path
        except (OSError, ValueError, RuntimeError):
            error(label, "resolves outside the build root or cannot be resolved")
            return None

    def asset_record(record, label, size, role):
        record = object_at(record, label)
        if record is None:
            return
        path = safe_path(record.get("path"), label + ".path")
        digest = record.get("sha256")
        if not isinstance(digest, str) or re.fullmatch(r"[0-9a-fA-F]{64}", digest) is None:
            error(label + ".sha256", "must be a 64-character hexadecimal SHA-256")
            digest = None
        if path is not None and size is not None:
            jobs.append((path, label, size, role, digest))

    def mask_path(value, label, size, role):
        path = safe_path(value, label)
        if path is not None and size is not None:
            jobs.append((path, label, size, role, None))

    scene = object_at(manifest, "manifest")
    if scene is not None:
        if type(scene.get("schemaVersion")) is not int or scene.get("schemaVersion") != 1:
            error("schemaVersion", "must be integer 1")
        text_at(scene.get("id"), "id")
        canvas = object_at(scene.get("canvas"), "canvas")
        if canvas is not None:
            for key, expected in (("width", 1536), ("height", 1024), ("pixelsPerCell", 32)):
                if type(canvas.get(key)) is not int or canvas.get(key) != expected:
                    error("canvas." + key, f"must be integer {expected}")
            for key, expected in (("imageGroundOrigin", [0, 224]), ("regionOrigin", [16, 0])):
                if not _vector(canvas.get(key), 2, True) or canvas.get(key) != expected:
                    error("canvas." + key, f"must be {expected}")
        for key in ("reference", "baseline", "base"):
            asset_record(scene.get(key), key, (1536, 1024), "canvas")
        base = scene.get("base")
        if isinstance(base, dict):
            flag(base.get("retainsStatic"), "base.retainsStatic")
            mask_path(base.get("inferenceMaskPath"), "base.inferenceMaskPath", (1536, 1024), "inference-mask")
        report = object_at(scene.get("report"), "report")
        if report is not None:
            safe_path(report.get("path"), "report.path")
        components = scene.get("components")
        if not isinstance(components, list):
            error("components", "must be a list")
            components = []
        counts["componentCount"] = len(components)
        ids = set()
        for index, entry in enumerate(components):
            label = f"components[{index}]"
            entry = object_at(entry, label)
            if entry is None:
                continue
            component_id = entry.get("id")
            if text_at(component_id, label + ".id"):
                if component_id != component_id.strip():
                    error(label + ".id", "must not have surrounding whitespace")
                if component_id in ids:
                    error(label + ".id", "duplicate component ID: " + component_id)
                ids.add(component_id)
            for key in ("name", "kind"):
                text_at(entry.get(key), label + "." + key)
            for key in ("mutable", "contributesToComposite", "defaultVisible"):
                flag(entry.get(key), label + "." + key)
            counts["mutableCount"] += int(entry.get("mutable") is True)
            counts["contributingCount"] += int(entry.get("contributesToComposite") is True)
            if "interaction" not in entry or (entry["interaction"] is not None and not _text(entry["interaction"])):
                error(label + ".interaction", "must be a nonempty string or null")
            if not _number(entry.get("depth")):
                error(label + ".depth", "must be finite and numeric, not boolean")
            bounds = entry.get("bounds")
            size = None
            if not _vector(bounds, 4, True):
                error(label + ".bounds", "must contain four integer coordinates/extents")
            else:
                x, y, width, height = bounds
                if x < 0 or y < 0 or width <= 0 or height <= 0 or x + width > 1536 or y + height > 1024:
                    error(label + ".bounds", "must have positive size and lie within the source canvas")
                else:
                    size = (width, height)
            foot = entry.get("foot")
            if not _vector(foot, 2) or not (0 <= foot[0] <= 1536 and 0 <= foot[1] <= 1024):
                error(label + ".foot", "must be a finite source-canvas point")
            pivot = entry.get("pivot")
            if not _vector(pivot, 2) or not all(0 <= value <= 1 for value in pivot):
                error(label + ".pivot", "must be a finite normalized pair in [0,1]")
            footprint = entry.get("footprintCells")
            if not isinstance(footprint, list):
                error(label + ".footprintCells", "must be a list")
            else:
                seen_cells = set()
                for cell_index, cell in enumerate(footprint):
                    cell_label = label + f".footprintCells[{cell_index}]"
                    if not _vector(cell, 2, True) or not (16 <= cell[0] < 64 and 0 <= cell[1] < 25):
                        error(cell_label, "must be an integer cell inside x[16,64), y[0,25)")
                        continue
                    key = tuple(cell)
                    if key in seen_cells:
                        error(cell_label, "duplicate footprint cell")
                    seen_cells.add(key)
                if entry.get("mutable") is True and not footprint:
                    warnings.append(label + ": mutable component has no footprint binding; Unity interaction remains unbound")
            asset_record(entry.get("sprite"), label + ".sprite", size, "cutout")
            if "contact" not in entry:
                error(label + ".contact", "must be an asset object or null")
            elif entry["contact"] is not None:
                asset_record(entry["contact"], label + ".contact", size, "cutout")
            repair = object_at(entry.get("repair"), label + ".repair")
            if repair is not None:
                mask_path(repair.get("maskPath"), label + ".repair.maskPath", size, "repair-mask")
                text_at(repair.get("surface"), label + ".repair.surface")
                flag(repair.get("inferred"), label + ".repair.inferred")
            extraction = object_at(entry.get("extraction"), label + ".extraction")
            if extraction is not None:
                for key in ("method", "quality"):
                    text_at(extraction.get(key), label + ".extraction." + key)
                notes = extraction.get("notes")
                if not isinstance(notes, list) or not all(isinstance(note, str) for note in notes):
                    error(label + ".extraction.notes", "must be a list of strings")

    # Do not follow/read any asset if structural or confinement validation already failed.
    checked = 0
    if require_assets and not errors:
        try:
            from PIL import Image
        except ImportError:
            errors.append("strict asset validation requires Pillow")
        else:
            for path, label, expected_size, role, digest in jobs:
                try:
                    raw = path.read_bytes()
                    if digest is not None and hashlib.sha256(raw).hexdigest() != digest.lower():
                        error(label, "SHA-256 does not match file bytes")
                    with Image.open(io.BytesIO(raw)) as image:
                        if image.format != "PNG":
                            error(label, "asset must be a PNG")
                            continue
                        if image.size != expected_size:
                            error(label, f"image dimensions {image.size} do not match {expected_size}")
                            continue
                        image.load()
                        if role == "cutout":
                            if "A" not in image.getbands() and "transparency" not in image.info:
                                error(label, "cutout requires actual alpha transparency")
                            else:
                                alpha_min, alpha_max = image.convert("RGBA").getchannel("A").getextrema()
                                if alpha_min != 0 or alpha_max != 255:
                                    error(label, "cutout must contain both fully transparent and fully opaque pixels")
                        elif role in ("repair-mask", "inference-mask"):
                            if image.mode in ("1", "L"):
                                extent = image.convert("L").getextrema()
                            elif "A" in image.getbands():
                                extent = image.getchannel("A").getextrema()
                            else:
                                error(label, "mask must be grayscale or carry an explicit alpha channel")
                                continue
                            if role == "repair-mask" and extent[1] == 0:
                                error(label, "repair mask is empty")
                    checked += 1
                except (OSError, ValueError, SyntaxError, Image.DecompressionBombError) as exc:
                    error(label, f"asset cannot be read safely: {exc}")
    return {
        "ok": not errors, "valid": not errors, "errors": errors, "warnings": warnings,
        **counts, "assetChecks": "strict" if require_assets else "skipped", "assetsChecked": checked,
    }


def _state_inputs(manifest, removed_ids, requested_ids):
    report = validate_manifest(manifest)
    if not report["ok"]:
        raise ManifestValidationError("Invalid manifest: " + "; ".join(report["errors"]))
    components = {component["id"]: component for component in manifest["components"]}

    def ids(value, name):
        if isinstance(value, (str, bytes, Mapping)) or not isinstance(value, Iterable):
            raise ManifestValidationError(name + " must be a collection of component ID strings")
        result = set()
        for item in value:
            if not _text(item):
                raise ManifestValidationError(name + " contains a non-string or empty ID")
            if item not in components:
                raise ManifestValidationError(name + " contains unknown ID: " + item)
            component = components[item]
            if not component["mutable"] or not component["contributesToComposite"]:
                raise ManifestValidationError("Component does not support removal state: " + item)
            result.add(item)
        return frozenset(result)

    # Resolve/validate both sets entirely before the caller computes a new immutable state.
    return ids(removed_ids, "removed_ids"), ids(requested_ids, "requested_ids")


def remove_components(manifest, removed_ids, requested_ids):
    """Return the union after validating the entire request; never mutate the manifest/state."""
    removed, requested = _state_inputs(manifest, removed_ids, requested_ids)
    return removed | requested


def restore_components(manifest, removed_ids, requested_ids):
    """Return the difference; restoration is an offline state operation, not a game ability."""
    removed, requested = _state_inputs(manifest, removed_ids, requested_ids)
    return removed - requested
