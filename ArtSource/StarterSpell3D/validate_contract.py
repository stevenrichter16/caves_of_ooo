#!/usr/bin/env python3
"""Validate independently sampled Blender assets and FBX readback, not recipe intent."""
import argparse, copy, hashlib, json
from pathlib import Path

ROOT = Path(__file__).resolve().parent
EXPECTED = ['ember_spit', 'flaming_hands', 'jet_blast', 'ground_surge', 'rime_grip', 'calm', 'conjure_rain']

def check(data, root=ROOT):
    errors = []
    def need(condition, message):
        if not condition: errors.append(message)
    need((root/'starter_spells.blend').is_file(), 'editable Blender study is absent')
    need(data.get('spellIds') == EXPECTED, 'starter/utility identities or order changed')
    need(data.get('cameraPitchDegrees') == 56, 'camera pitch is not current 56 degrees')
    need(data.get('unitsPerCell') == 1, 'one metre per native cell required')
    need(data.get('sourceShaBefore') == data.get('sourceShaAfter'), 'existing player source changed')
    rows = data.get('studies', [])
    need(len(rows) == 7, 'expected six starters and one separate utility')
    need([row.get('id') for row in rows] == EXPECTED, 'sampled study identities mismatch')
    for row in rows:
        prefix = row.get('id', '?')+': '
        need(row.get('actorRootDrift', 999) < 1e-6, prefix+'caster root moved')
        need(row.get('gesturePoseDelta', 0) > .1, prefix+'empty caster gesture')
        need(row.get('animatedMeshCount', 0) > 0, prefix+'no animated effect meshes')
        need(row.get('effectTransformDelta', 0) > .2, prefix+'effect action is empty/static')
        need(row.get('effectVisibleAtContact', 0) > 0, prefix+'effect absent at contact')
        need(row.get('effectVisibleAfterClear', 99) == 0, prefix+'effect fails to clear')
        need(row.get('colliderCount', 1) == 0, prefix+'design meshes must not have gameplay colliders')
        need(row.get('materialAlphaMinimum', 0) == 1, prefix+'solid Principled alpha changed (explicit glow gradients audited separately)')
        need(row.get('materialAnimatedCount', 1) == 0, prefix+'material animation is not portable')
        need(row.get('exportExists', False), prefix+'FBX is missing')
        need(row.get('roundtripBoneCount', 0) == 9, prefix+'FBX skeleton is incomplete')
        need(row.get('roundtripAnimatedObjectCount', 0) >= 2, prefix+'FBX animation lost')
        need(row.get('roundtripMeshCount', 0) >= row.get('animatedMeshCount', 999), prefix+'FBX mesh geometry lost')
        need(row.get('roundtripMotionDelta', 0) > .2, prefix+'FBX effects do not move')
        need(row.get('roundtripEffectMotionDelta', 0) > .2, prefix+'FBX effect-specific transform animation lost')
        need(row.get('roundtripEffectCount', -1) == row.get('animatedMeshCount'), prefix+'FBX transient effect identities lost')
        need(row.get('roundtripEffectVisibleAfterClear', 1) == 0, prefix+'FBX transient geometry does not clear')
        need(row.get('roundtripRootDrift', 999) < 1e-6, prefix+'FBX caster root drifts')
    return errors

def main():
    parser=argparse.ArgumentParser(); parser.add_argument('--adversarial', action='store_true'); args=parser.parse_args()
    report=ROOT/'asset-audit.json'
    if not report.exists():
        print(json.dumps({'status':'RED','errors':['asset-audit.json absent','editable Blender study absent']},indent=2)); return 1
    data=json.loads(report.read_text()); errors=check(data)
    controls=[]
    if args.adversarial and not errors:
        mutations=[('missing identity', lambda x:x['spellIds'].pop()),
            ('incorrect camera',lambda x:x.update(cameraPitchDegrees=90)),
            ('source modified',lambda x:x.update(sourceShaAfter='changed')),
            ('empty pose',lambda x:x['studies'][0].update(gesturePoseDelta=0)),
            ('empty effect action',lambda x:x['studies'][1].update(effectTransformDelta=0)),
            ('root motion',lambda x:x['studies'][2].update(actorRootDrift=.5)),
            ('uncleared mesh',lambda x:x['studies'][3].update(effectVisibleAfterClear=1)),
            ('missing export',lambda x:x['studies'][4].update(exportExists=False)),
            ('missing imported bone',lambda x:x['studies'][5].update(roundtripBoneCount=8)),
            ('empty imported clip',lambda x:x['studies'][6].update(roundtripMotionDelta=0))]
        for name, mutate in mutations:
            bad=copy.deepcopy(data); mutate(bad); caught=bool(check(bad)); controls.append({'case':name,'rejected':caught})
            if not caught: errors.append('vacuous validator: '+name)
    print(json.dumps({'status':'GREEN' if not errors else 'RED','studies':len(data.get('studies',[])),'errors':errors,'adversarialControls':controls},indent=2))
    return int(bool(errors))

if __name__=='__main__': raise SystemExit(main())
