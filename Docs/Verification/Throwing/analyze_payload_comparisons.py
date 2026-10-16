#!/usr/bin/env python3
"""Summarize native W3/W7 receipts without pretending scripted policies are optimal."""
import collections
import csv
import gzip
import json
from pathlib import Path
import statistics
import xml.etree.ElementTree as ET

BASE = Path(__file__).resolve().parent
DATA = BASE / 'comparisons'
EXPECTED = {f'{family}-budget-{budget}.json' for family in ('W3', 'W7', 'W7-order') for budget in (0, 2, 4, 6)}

def mean(rows, fn):
    return round(statistics.mean(fn(row) for row in rows), 3)

def supply(text):
    return sum(int(item.split(':')[1]) for item in text.split(';') if ':' in item)

def used_focused(row):
    # A policy request is not an executed launch (notably absent spare stock),
    # and order experiments intentionally log actual commands as their authority.
    return any(':focused:paid' in command for command in row['common']['commands'])

def run():
    groups = collections.defaultdict(list)
    files = []
    stale = []
    candidates = {}
    for path in sorted(DATA.glob('W[37]*-budget-*.json*')):
        name = path.name.removesuffix('.gz')
        if name not in EXPECTED:
            continue
        # A later native rerun writes plain JSON: prefer it to the prior archive.
        if name not in candidates or path.suffix != '.gz': candidates[name] = path
    for name, path in sorted(candidates.items()):
        files.append(name)
        with (gzip.open(path, 'rt') if path.suffix == '.gz' else path.open()) as stream:
            source_rows = json.load(stream)['trials']
        budget = int(name.removesuffix('.json').rsplit('-', 1)[1]); packages = {0:1,2:5,4:4,6:4}[budget]
        expected_rows = packages * (700 if name.startswith('W7-budget') else 120)
        if len(source_rows) != expected_rows: stale.append(f'{name}: {len(source_rows)}/{expected_rows} trials')
        for row in source_rows:
            c = row['common']
            assert c['spent'] + c['banked'] == c['budget'], (path, c)
            key = (c['scenario'], c['budget'], c['package'], c['variant'], c['policy'])
            groups[key].append(row)
    columns = ['scenario', 'budget', 'package', 'variant', 'policy', 'trials', 'survivors', 'actual_focused', 'paid_actions', 'enemy_turns', 'player_hp_loss', 'player_hp_delta', 'ally_hp_delta', 'enemy_hp_delta', 'ally_survivors', 'ally_hostile', 'remaining_items', 'used_or_lost_items', 'initial_film_units', 'remaining_film_units', 'crossed', 'detour', 'coated', 'outcomes']
    records = []
    for key, rows in sorted(groups.items()):
        seeds = [r['common']['seed'] for r in rows]
        assert sorted(seeds) == list(range(101, 121)), (key, seeds)
        records.append(dict(zip(columns, [*key, len(rows), sum(r['common']['survived'] for r in rows), sum(used_focused(r) for r in rows),
            mean(rows, lambda r: r['common']['actions']), mean(rows, lambda r: r['common']['enemyTurns']), mean(rows, lambda r: r['common']['hpLoss']), mean(rows, lambda r: r['common']['hp'] - r['common']['initialHp']),
            mean(rows, lambda r: r['allyHpDelta']), mean(rows, lambda r: r['enemyHpDelta']), mean(rows, lambda r: r['allySurvivors']), sum(r['hostileAlly'] for r in rows),
            mean(rows, lambda r: supply(r['common']['finalSupply'])), mean(rows, lambda r: supply(r['common']['initialSupply']) + len(r['common']['supplied']) - supply(r['common']['finalSupply'])),
            mean(rows, lambda r: r['matterAfterAction']), mean(rows, lambda r: r['remainingMatter']), sum(r['crossed'] for r in rows), mean(rows, lambda r: r['maxDetour']), sum(r['coated'] for r in rows),
            '; '.join(f'{k}:{v}' for k, v in sorted(collections.Counter(r['common']['outcome'] for r in rows).items()))])))
    output = BASE / 'payload-comparison-summary.csv'
    with output.open('w', newline='') as stream:
        writer = csv.DictWriter(stream, fieldnames=columns); writer.writeheader(); writer.writerows(records)
    missing = sorted(EXPECTED - set(files))
    terminal = BASE / 'explicit-stream-comparisons-green.xml'
    terminal_attributes = ET.parse(terminal).getroot().attrib if terminal.exists() else {}
    accepted = all(terminal_attributes.get(k) == v for k, v in
                   {'result': 'Passed', 'total': '28', 'passed': '28', 'failed': '0'}.items())
    provenance = ('**Accepted corrected checkpoint.** W7 ground/order tables use the corrected explicit-stream native run in '
        '`explicit-stream-comparisons-green.xml` (28/28 passed). W3 retains the earlier seeded-tonic native receipts from '
        '`seeded-comparisons-green.xml`; W3 was not rerun in that 28-case set. The test-only witness preserves explicitly supplied '
        'combat/brain RNG streams and seeds otherwise unowned stun saves; production RNG is unchanged. Prior provisional and '
        'overwritten-stream checkpoints are retained separately and are not the source of these current W7 tables.' if accepted else
        '**Checkpoint scope — corrected terminal gate not established.** These tables are descriptive receipts only; do not treat '
        'them as accepted corrected fixed-seed comparisons until the explicit-stream terminal run and its provenance are verified.')
    text = ['# Controlled payload comparison results', '', provenance, '', f'Native receipt files: {len(files)}/12. Trials: {sum(r["trials"] for r in records)}. Each row summarizes exactly seeds101–120.', '',
        'These are controlled supplied arenas with actual purchases, commands, finite stock and ordinary AI. They do not establish natural acquisition, general win rates, optimal policies or physical Steam Deck behavior. Level budgets change actual earned-level stats equally within each budget. The same seed does not force identical later RNG draws after different actions.', '',
        'W3 measures two wounded allied layouts with finite starting healing. W7 measures lane holding, route detours and early retreat separately. W7-order pays the intervening real enemy and terrain turns; an unavailable or blocked follow-up is recorded as refused rather than made legal. Reported item use also includes a lost carried/equipped unit, so refer to custody and commands before treating it as consumed. The CSV actual_focused column counts trials with a paid Focused launch from the actual command receipt; the raw policy flag alone does not establish use.', '']
    if stale:
        text += ['**Earlier smoke/incomplete policy matrix:** ' + ', '.join(stale), '']
    if missing:
        text += ['**Incomplete matrix; missing:** ' + ', '.join(missing), '']
    for title, scenario, policy, variant in [('Focused healing tradeoff', 'W3', 'focused-if-owned', None), ('Ground control with ordinary Broad splash', 'W7', 'broad-ground', None), ('Ground control with Focused when purchased', 'W7', 'focused-if-owned', None), ('Order comparisons', 'W7-order', None, None)]:
        selected = [r for r in records if r['scenario'] == scenario and (policy is None or r['policy'] == policy) and r['package'] in ('bank', 'focused', 'general4', 'all5-bank', 'focus-water')]
        text += [f'## {title}', '', '| Budget/package | Variant/policy | Alive | Ally ΔHP | Enemy ΔHP | Player HP lost | Actions | Crossed | Detour | Used/lost items |', '| --- | --- | ---: | ---: | ---: | ---: | ---: | ---: | ---: | ---: |']
        for r in selected:
            text.append(f'| {r["budget"]}/{r["package"]} | {r["variant"]}/{r["policy"]} | {r["survivors"]}/20 | {r["ally_hp_delta"]} | {r["enemy_hp_delta"]} | {r["player_hp_loss"]} | {r["paid_actions"]} | {r["crossed"]}/20 | {r["detour"]} | {r["used_or_lost_items"]} |')
        text.append('')
    (BASE / 'payload-comparison-summary.md').write_text('\n'.join(text) + '\n')
    print(f'{len(files)}/12 files; {sum(r["trials"] for r in records)} trials; {len(records)} aggregate rows; missing={missing}; stale={stale}')

if __name__ == '__main__':
    run()
