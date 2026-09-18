def validate_walk_steps(phase,steps,index):
    """Accept exact native moves and explicitly identified unsuccessful attempts."""
    errors=[]
    def need(ok,message):
        if not ok:errors.append(f'phase{index} '+message)
    counts={'moved':0,'target_rejected':0,'input_without_movement':0};consecutive_unmoved=0;previous=None
    integers=['phase','targetX','targetY','beforeX','beforeY','afterX','afterY','beforeTick','afterTick','beforeHp','afterHp']
    flags=['pass','inputIssued','targetAvailable','targetExcluded']
    for n,s in enumerate(steps):
        valid=isinstance(s,dict) and all(type(s.get(k)) is int for k in integers) and all(type(s.get(k)) is bool for k in flags)
        need(valid,f'attempt{n} missing/invalid adaptive schema')
        if not valid:continue
        outcome=s.get('outcome');need(outcome in counts,f'attempt{n} unknown/incomplete outcome')
        if outcome not in counts:continue
        counts[outcome]+=1
        need(s.get('zoneId')==phase['zoneId'] and s['phase']==index,f'attempt{n} identity')
        seconds=s.get('seconds');need(type(seconds) in (int,float) and phase['startSeconds']<=seconds<=phase['startSeconds']+phase['seconds'],f'attempt{n} outside phase')
        need(s.get('inputState')=='Normal' and s['beforeHp']>0 and s['afterHp']>0,f'attempt{n} unhealthy/state')
        before=(s['beforeX'],s['beforeY']);after=(s['afterX'],s['afterY']);target=(s['targetX'],s['targetY'])
        delta=(target[0]-before[0],target[1]-before[1]);keys={(1,0):'D',(-1,0):'A',(0,1):'S',(0,-1):'W'}
        need(delta in keys and s.get('key')==keys.get(delta),f'attempt{n} key/target is not cardinal')
        need(0<target[0]<79 and 0<target[1]<24 and 0<before[0]<79 and 0<before[1]<24,f'attempt{n} crosses native border')
        if previous is None:
            need(before==(phase['startX'],phase['startY']) and s['beforeTick']==phase['startTick'],f'attempt{n} start receipt')
        else:
            need(before==(previous['afterX'],previous['afterY']) and s['beforeTick']==previous['afterTick'],f'attempt{n} discontinuous native path')
            need(type(seconds) in (int,float) and type(previous.get('seconds')) in (int,float) and seconds>=previous['seconds'],f'attempt{n} time moved backwards')
        previous=s
        if outcome=='target_rejected':
            need(not s['inputIssued'] and not s['pass'] and (not s['targetAvailable'] or s['targetExcluded']),f'attempt{n} false preflight rejection')
            need(after==before and s['afterTick']==s['beforeTick'] and s['afterHp']==s['beforeHp'],f'attempt{n} no-input record changed native state')
        else:
            need(s['inputIssued'] and s['targetAvailable'] and not s['targetExcluded'],f'attempt{n} invalid issued-input preflight')
            if outcome=='moved':
                need(s['pass'] and after==target and s['afterTick']>s['beforeTick'],f'attempt{n} false successful step')
                consecutive_unmoved=0
            else:
                consecutive_unmoved+=1
                need(not s['pass'] and after==before and s['afterTick']>=s['beforeTick'],f'attempt{n} false unmoved-input receipt')
                need(consecutive_unmoved<4,f'attempt{n} exceeded four-unmoved abort bound')
    need(counts['moved']==phase.get('successfulSteps') and counts['moved']>=2,'native successful step count mismatch')
    need(counts['target_rejected']==phase.get('rejectedTargets'),'rejected-target count mismatch')
    need(counts['input_without_movement']==phase.get('unmovedInputs'),'unmoved-input count mismatch')
    need(counts['moved']+counts['input_without_movement']==phase.get('inputAttempts'),'issued-input count mismatch')
    if previous is not None:
        need((previous['afterX'],previous['afterY'])==(phase['endX'],phase['endY']) and previous['afterTick']==phase['endTick'],'final step/phase endpoint mismatch')
    return errors
