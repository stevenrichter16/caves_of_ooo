from pathlib import Path
import json
p=Path(__file__).resolve().parent
labels=['before-repair','before-start','working','ready','collected-reloaded','eaten']
d={n:json.loads((p/f'kitchen-service-65-job-{n}.json').read_text()) for n in labels}
b,s,w,r,c,e=[d[n] for n in labels]
checks={
 'repair_consumed_two_actual_clay':not b['pan']['repaired'] and s['pan']['repaired'] and sum(i['count'] for i in b['inventory'] if i['blueprint']=='FireClay')==2 and not any(i['blueprint']=='FireClay' for i in s['inventory']),
 'payment_two_drams':(s['player']['drams'],w['player']['drams'],s['cook']['drams'],w['cook']['drams'])==(50,48,100,102),
 'actual_recipe_escrow':sorted((i['blueprint'],i['count']) for i in w['escrow']['rows'])==[('ClaspbeanPulp',1),('Emberwheat',2)] and all(i['custodyMatchesExpected'] and i['escrowRefs']==1 and i['escrowSameIds']==1 for i in w['escrow']['rows']),
 'due_120_ordinary_ticks':w['job']['dueTick']-w['job']['startTick']==120 and w['tick']==5120 and r['tick']==5230,
 'one_commissioned_ready_output':r['job']['state']=='Ready' and r['pickup']['rowCount']==1 and r['escrow']['rowCount']==0 and r['job']['output']['id']=='9249' and r['job']['output']['custodyMatchesExpected'] and r['job']['output']['commissionStation']=='7208' and r['job']['output']['commissionCustomer']=='2696' and r['job']['output']['commissionOrdinal']==1,
 'same_meal_carried_after_reload':any(i['id']=='9249' and i['blueprint']=='FieldMeal' and i['count']==1 and i['custodyMatchesExpected'] and i['commissionOrdinal']==1 for i in c['inventory']) and c['job']['state']=='Idle' and c['job']['ordinal']==1 and c['pickup']['rowCount']==0 and c['escrow']['rowCount']==0 and c['pan']['repaired'] and c['player']['drams']==48 and c['cook']['drams']==102,
 'one_consumed_without_replacement':not any(i['id']=='9249' or i['blueprint']=='FieldMeal' for i in e['inventory']) and e['tick']==c['tick']+10 and e['pickup']['rowCount']==0 and e['escrow']['rowCount']==0 and e['job']['state']=='Idle' and e['job']['ordinal']==1,
 'current_flat_graph_at_each_receipt':all(v['flat'] and v['currentGraphMatches'] and v['managerHasZone'] for v in d.values()),
 'full_hp_consumption_only':all(v['player']['hp']==v['player']['maxHp']==40 for v in d.values())}
assert all(checks.values()),checks
out={'kind':'ordinary_keyboard_service_acceptance','seed':65,'checks':checks,'action_record':'kitchen-service-play-record.md','raw_receipts':[f'kitchen-service-65-job-{n}.json' for n in labels],'limitations':['Initial failed east-grain attempt/death retained; second route uses actual glade grain and cellar clay.','Full HP consumption establishes no measured healing or bleed removal.','Existing neutral service world art is not visual proof of job state; paired reader and custody receipts establish it.','This predates new kitchen-state integration; no new runtime tests are claimed by this summary.','Editor keyboard, not packaged or physical Steam Deck.']}
(p/'kitchen-service-play-summary.json').write_text(json.dumps(out,indent=2)+'\n')
print(json.dumps(checks,indent=2))
