import ast,copy,pathlib,unittest
path=pathlib.Path(__file__).with_name('recompute_profile.py')
def load():
    tree=ast.parse(path.read_text());functions=[n for n in tree.body if isinstance(n,ast.FunctionDef) and n.name=='validate_walk_steps']
    if not functions:return lambda *args:['missing strict adaptive receipt validator']
    ns={};exec(compile(ast.Module(body=functions,type_ignores=[]),str(path),'exec'),ns);return ns['validate_walk_steps']
def fixtures():
    phase=dict(zoneId='Overworld.3.7.0',kind='walk',startSeconds=0,seconds=5,startTick=10,endTick=12,startX=10,startY=10,endX=10,endY=10,successfulSteps=2,inputAttempts=3,rejectedTargets=1,unmovedInputs=1)
    base=dict(zoneId=phase['zoneId'],phase=2,key='D',inputState='Normal',outcome='target_rejected',pass_=False,inputIssued=False,targetAvailable=False,targetExcluded=False,seconds=.1,targetX=11,targetY=10,beforeX=10,beforeY=10,afterX=10,afterY=10,beforeTick=10,afterTick=10,beforeHp=20,afterHp=20)
    base['pass']=base.pop('pass_'); rejected=base.copy()
    unmoved=dict(base,inputIssued=True,targetAvailable=True,outcome='input_without_movement',seconds=.2)
    move=dict(base,key='S',inputIssued=True,targetAvailable=True,outcome='moved',seconds=.3,targetX=10,targetY=11,afterX=10,afterY=11,afterTick=11);move['pass']=True
    back=dict(move,key='W',seconds=.4,beforeX=10,beforeY=11,targetY=10,afterY=10,beforeTick=11,afterTick=12)
    return phase,[rejected,unmoved,move,back]
class ReceiptTests(unittest.TestCase):
    def test_honest_mixed_attempts_pass(self):
        p,s=fixtures();self.assertEqual([],load()(p,s,2))
    def test_missing_schema_rejected(self):
        p,s=fixtures();del s[0]['inputIssued'];self.assertTrue(load()(p,s,2))
    def test_failed_attempt_cannot_count_as_success(self):
        p,s=fixtures();p['successfulSteps']=4;self.assertTrue(load()(p,s,2))
    def test_preflight_cannot_claim_input(self):
        p,s=fixtures();s[0]['inputIssued']=True;self.assertTrue(load()(p,s,2))
    def test_preflight_cannot_advance_or_move(self):
        for field in ['afterTick','afterX','afterHp']:
            p,s=fixtures();s[0][field]+=1;self.assertTrue(load()(p,s,2),field)
    def test_blocked_input_cannot_claim_movement(self):
        p,s=fixtures();s[1]['afterX']+=1;self.assertTrue(load()(p,s,2))
    def test_success_requires_exact_target_and_tick(self):
        for field,value in [('afterX',11),('afterTick',10),('targetX',11),('key','A')]:
            p,s=fixtures();s[2][field]=value;self.assertTrue(load()(p,s,2),field)
    def test_unknown_or_pending_outcome_rejected(self):
        for value in ['unknown','pending_input','invalid_native_result']:
            p,s=fixtures();s[0]['outcome']=value;self.assertTrue(load()(p,s,2))
    def test_wrong_zone_or_phase_rejected(self):
        for field,value in [('zoneId','Elsewhere'),('phase',3)]:
            p,s=fixtures();s[0][field]=value;self.assertTrue(load()(p,s,2))
    def test_wrong_counters_rejected(self):
        for field in ['inputAttempts','rejectedTargets','unmovedInputs']:
            p,s=fixtures();p[field]+=1;self.assertTrue(load()(p,s,2))
    def test_unhealthy_or_wrong_state_rejected(self):
        for field,value in [('afterHp',0),('inputState','Targeting'),('beforeHp',0)]:
            p,s=fixtures();s[1][field]=value;self.assertTrue(load()(p,s,2))
    def test_step_time_outside_phase_rejected(self):
        p,s=fixtures();s[0]['seconds']=6;self.assertTrue(load()(p,s,2))
if __name__=='__main__':unittest.main()
