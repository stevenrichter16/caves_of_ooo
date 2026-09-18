"""Result-only validation. No editor, process management, network, or mutations."""
from collections import Counter
import csv
import hashlib
import json
import math
from pathlib import Path
import re
import struct

GPU_CASES = '''01_palette_visible_unseen
02_wrong_dimensions_and_default_fail_closed
03_transient_memory_vs_visible
04_one_mesh_crosses_cell_mask
05_memory_ignores_live_light_and_caster
06_memory_obeys_native_rgb
07_water_memory_ignores_real_elapsed_time
08_visible_caster_produces_real_shadow
09_remembered_unseen_casters_do_not_shadow
10_water_visibility_symmetry
11_renderer2d_composite_actual_rt_orientation
12_gameplay_exposure_preserves_darkness_and_light_ratios'''.splitlines()
NATIVE_CASES = '''native_requested_world_seed
normal_bootstrap_started
native_new_game_starts_in_authored_morrowfast
native_new_game_starts_at_south_road_40_23
native_new_game_six_exact_authored_garden_terrain
native_new_game_garden_tag_index_matches_six_refs
native_new_game_only_garden_plantable_road_excluded
all_71_actual_source_owners
actual_requested_presentation_ready
native_F5_private_checkpoint
three_nonvacuous_25_second_phases
native_3d_owner_graph_after_profile
native_enters_actual_room
native_3d_entered_roof_and_visible_interior
native_manual_roof_actual_state
native_3d_explicit_lift_hides_roof_after_lift
native_loot_exact_units
native_inventory_drop_places_real_item
native_pickup_restores_exact_inventory_units
native_3d_exact_auto_equipped_dagger_after_pickup
native_save_load_rebuilds_actual_graph
native_3d_owner_graph_after_F6
native_3d_F6_fresh_player_view
native_3d_exact_auto_equipped_dagger_after_F6
native_3d_explicit_lift_hides_roof_after_F6
native_zone_exit_disables_village_view
native_zone_reentry_retains_actual_state
native_3d_empty_crate_clear
native_F11_original_view
native_F11_restores_3d
native_shift_F11_actual_low_detail
native_display_controls_take_no_turn
native_spell_escape_no_cast
native_spell_real_cast_fx_and_turn
final_private_scope'''.splitlines()
SCREEN_LABELS = ('new-game-start', 'initial', 'interior', 'lifted-roof', 'low-detail', 'native-jet-blast', 'final')


def digest(path):
    h = hashlib.sha256()
    with Path(path).open('rb') as f:
        for chunk in iter(lambda: f.read(1024 * 1024), b''):
            h.update(chunk)
    return h.hexdigest()


def load(path):
    value = json.loads(Path(path).read_text())
    if not isinstance(value, dict):
        raise ValueError('Expected JSON object: ' + str(path))
    return value


def guid(value):
    return isinstance(value, str) and bool(re.fullmatch(r'[0-9a-f]{32}', value)) and value != '0' * 32


def png_size(path):
    with Path(path).open('rb') as f:
        head = f.read(24)
    if len(head) != 24 or head[:8] != b'\x89PNG\r\n\x1a\n' or head[12:16] != b'IHDR':
        raise ValueError('Not a PNG with an IHDR: ' + str(path))
    return struct.unpack('>II', head[16:24])


def add(rows, name, passed, detail=''):
    rows.append({'check': name, 'pass': bool(passed), 'detail': detail})


def validate_gpu(run):
    run = Path(run).resolve(); report_path = run / 'artifacts/shader-gpu.json'
    d = load(report_path); rows = []
    add(rows, 'gpu_fresh_report_location', Path(d.get('reportPath', '')).resolve() == report_path)
    add(rows, 'gpu_run_id', guid(d.get('runId')), d.get('runId'))
    add(rows, 'gpu_status_and_counts', d.get('status') == 'PASS' and d.get('passed') == 12 and d.get('failed') == 0 and not d.get('error'))
    cases = d.get('cases') or []
    add(rows, 'gpu_exact_12_cases', Counter(c.get('id') for c in cases) == Counter(GPU_CASES))
    add(rows, 'gpu_all_case_assertions', len(cases) == 12 and all(c.get('passed') is True and not c.get('error') for c in cases))
    add(rows, 'gpu_no_scoped_errors', d.get('unexpectedLogs') == [])
    for key in ('activeScenePreserved', 'sceneDirtyFlagsPreserved', 'renderTextureActiveRestored', 'asyncCompilationRestored'):
        add(rows, 'gpu_' + key, d.get(key) is True)
    add(rows, 'gpu_actual_render_configuration', (d.get('width'), d.get('height')) == (512, 512)
        and d.get('compositeRendererIndex') == 0 and d.get('worldRendererIndex', 0) > 0
        and d.get('graphicsApi') not in (None, '', 'Null', 'Unknown'))
    count = 0
    for case in cases:
        frames = case.get('frames') or []
        add(rows, 'gpu_frames_' + str(case.get('id')), bool(frames))
        for frame in frames:
            path = Path(frame.get('png', '')).resolve()
            safe = path.parent == report_path.parent and path.is_file() and not path.is_symlink()
            valid = safe and bool(re.fullmatch(r'[0-9a-f]{64}', frame.get('sha256', '')))  and png_size(path) == (512, 512)
            add(rows, 'gpu_png_' + str(case.get('id')) + '_' + str(frame.get('name')), valid, str(path))
            count += 1
    add(rows, 'gpu_nonempty_pixel_evidence', count >= 12, count)
    return rows


def validate_native(run, live, prior_run_ids, screen_map):
    run = Path(run).resolve(); live = Path(live).resolve(); artifacts = run / 'artifacts'
    d = load(artifacts / 'V3D-after-native.json'); c = load(artifacts / 'V3D-after-cleanup.json'); rows = []
    run_id = d.get('runId')
    add(rows, 'native_fresh_run_id', guid(run_id) and run_id not in prior_run_ids, run_id)
    add(rows, 'native_status_and_exact_35', d.get('mode') == 'after' and d.get('failures') == 0
        and d.get('unexpectedErrors') == 0 and d.get('cases') == 35 and not d.get('fatal'))
    add(rows, 'native_35_named_assertions', Counter(d.get('audit') or []) == Counter('PASS ' + n for n in NATIVE_CASES))
    for key in ('workloadComplete', 'shutdownObserved', 'shutdownRootHeld', 'shutdownSavingUnregistered', 'displayPreferencesRestored'):
        add(rows, 'native_' + key, d.get(key) is True)
    add(rows, 'native_no_overflow', d.get('overflow') is False)
    add(rows, 'native_authored_seed_start', d.get('requestedWorldSeed') == 729490642 and d.get('worldSeed') == 729490642
        and d.get('initialZoneId') == 'Overworld.3.6.0' and (d.get('initialX'), d.get('initialY')) == (40, 23))
    expected_garden = [f'{x},{y};terrainCount=1;id=morrowfast-terrain:{x}:{y};blueprint=TepuiStone;plantable=True;indexedRefs=1;valid=True'
                       for x in (41, 42) for y in (21, 22, 23)]
    add(rows, 'native_six_exact_garden_receipts', Counter(d.get('startingGarden') or []) == Counter(expected_garden))
    add(rows, 'native_real_screen_and_fx', (d.get('screenWidth'), d.get('screenHeight')) == (1920, 1080)
        and d.get('graphicsApi') not in (None, '', 'Null', 'Unknown')
        and d.get('nativeCastEvents', 0) > 0 and d.get('nativeFxAtomsObserved', 0) > 0)
    root = d.get('saveRoot')
    add(rows, 'cleanup_exact_run_root', c.get('runId') == run_id and c.get('mode') == 'after'
        and isinstance(root, str) and bool(root) and c.get('privateRoot') == root)
    safe_root = isinstance(root, str) and Path(root).is_absolute() and Path(root).parent.name == 'coo-native-save-audits' and guid(Path(root).name)
    add(rows, 'cleanup_private_root_really_removed', safe_root and not Path(root).exists(), root)
    for key in ('privateRootRemoved', 'scenesRestored', 'gameViewRestored', 'finalNativeVerified', 'seedSettingRestored'):
        add(rows, 'cleanup_' + key, c.get(key) is True)
    add(rows, 'cleanup_no_errors_or_nonzero_exit', c.get('nativeCleanupCode') == 0 and c.get('exitCode') == 0
        and all(not c.get(k) for k in ('sceneError', 'viewError', 'reportError')))
    add(rows, 'cleanup_seed_restoration', isinstance(c.get('oldRequestedSeed'), int)
        and c.get('oldRequestedSeed') == c.get('restoredRequestedSeed'))
    phases = d.get('phases') or []
    add(rows, 'native_three_measured_phases', Counter(p.get('name') for p in phases) == Counter(('idle', 'walk', 'door'))
        and all(25 <= p.get('seconds', -1) < 30 and p.get('frames', 0) > 50 for p in phases))
    add(rows, 'native_complete_frame_and_duration_totals', d.get('frames', 0) > 300
        and sum(p.get('frames', 0) for p in phases) == d.get('frames')
        and math.isclose(sum(p.get('seconds', 0) for p in phases), d.get('measuredSeconds', -1), abs_tol=0.0001))
    with (artifacts / 'V3D-after-frames.csv').open(newline='') as f:
        reader = csv.DictReader(f); fields = reader.fieldnames; frames = list(reader)
    add(rows, 'native_raw_csv_shape', fields is not None and fields[:4] == ['frame', 'seconds', 'phase', 'engine_frame_ms']
        and len(fields) >= 8 and len(frames) == d.get('frames') and all(None not in row for row in frames))
    add(rows, 'native_raw_csv_sequence', bool(frames) and all(int(row['frame']) == i for i, row in enumerate(frames)))
    add(rows, 'native_raw_csv_phase_counts', Counter(row['phase'] for row in frames) == Counter({p['name']: p['frames'] for p in phases}))
    add(rows, 'native_raw_csv_finite_time', bool(frames) and all(math.isfinite(float(row['seconds'])) and math.isfinite(float(row['engine_frame_ms']))
        and float(row['engine_frame_ms']) >= 0 for row in frames))
    screens = d.get('screenshots') or []
    expected_names = {f'V3D-after-{run_id}-{label}.png' for label in SCREEN_LABELS}
    add(rows, 'native_seven_exact_run_screens', len(screens) == 7 and {Path(s).name for s in screens} == expected_names)
    mapped = {row['original']: row for row in screen_map}
    for path_string in screens:
        path = Path(path_string); mapping = mapped.get(str(path)); target = Path(mapping['archive']) if mapping else None
        valid = path.parent.resolve() == live and path.name in expected_names and path.is_file() and not path.is_symlink()
        valid = valid and target is not None and target.parent.resolve() == artifacts / 'screens' and target.is_file()
        valid = valid and digest(path) == digest(target) == mapping['sha256'] and png_size(target) == (1920, 1080)
        add(rows, 'native_archived_png_' + path.name, valid)
    return rows
