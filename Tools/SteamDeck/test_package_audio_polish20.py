"""Synthetic offline gate tests. Never import Unity or package a player."""
import copy, datetime, hashlib, importlib.util, json, os, pathlib, struct, tempfile, unittest
SCRIPT=pathlib.Path(__file__).with_name('package-audio-polish20.py')
def load():
 s=importlib.util.spec_from_file_location('pack20',SCRIPT);m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m
def digest(p):return hashlib.sha256(p.read_bytes()).hexdigest()
def write(p,data):p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(data));return p
class Evidence(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.p=load()
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(prefix='coo-polish20-gates-');self.addCleanup(self.tmp.cleanup);self.root=pathlib.Path(self.tmp.name)
  self.contract=write(self.root/'routes.json',dict(schemaVersion=1,scope='audio-polish-twenty-routes',phases=[dict(iteration=n,mode=f'audio-polish-{n:02}',checks=['ordinary_player_and_native_zone',f'phase{n}_actual_action'],captures=['arrival.png',f'action-{n}.png']) for n in range(1,21)],sourceFiles=[]))
  src=self.root/'native-source.cs';src.write_text('// synthetic declared contract; not a real source run');c=json.loads(self.contract.read_text());c['sourceFiles']=[dict(path=src.name,sha256=digest(src))];write(self.contract,c)
  self.manifest=dict(schemaVersion=1,scope='audio-polish-twenty-iterations',notBeforeUtc='2026-10-11T00:00:00+00:00',iterations=[],history=[])
  for n in range(1,25):
   history=n>20;idx=n-20 if history else n;mode=('native-work-audio' if idx==3 else 'world-audio') if history else f'audio-polish-{idx:02}'
   folder=self.root/('history' if history else 'live')/str(idx);folder.mkdir(parents=True)
   shots=[]
   for name in ['arrival.png',f'action-{idx}.png']:
    path=folder/name;path.write_bytes(b'\x89PNG\r\n\x1a\n'+struct.pack('>I',13)+b'IHDR'+struct.pack('>II',1920,1080)+b'fixture-not-full-image')
    shots.append(dict(name=name,path=str(path),sha256=digest(path)))
   raw=dict(mode=mode,runId=f'fixture-{n}',complete=True,errorsFinalized=True,failures=0,unexpectedErrors=0,fatal='',cases=2,audit=['PASS ordinary_player_and_native_zone',f'PASS phase{idx}_actual_action'],seconds=5,screenshots=[x['path'] for x in shots],canVerify='Synthetic gate only',cannotVerify='Not live evidence')
   path=write(folder/'report.json',raw);stamp=datetime.datetime(2026,10,11,0,idx,tzinfo=datetime.timezone.utc);os.utime(path,(stamp.timestamp(),stamp.timestamp()))
   row=dict(iteration=idx,mode=mode,report=str(path),sha256=digest(path),runId=raw['runId'],timeBasis='report-file-mtime-estimate',reportModifiedUtc=stamp.isoformat(),screenshots=shots)
   self.manifest['history' if history else 'iterations'].append(row)
  self.file=self.root/'manifest.json';self.final=pathlib.Path(self.manifest['iterations'][-1]['report'])
 def validate(self):
  write(self.file,self.manifest);return self.p.validate_iterations(self.file,self.final,self.contract,digest(self.contract))
 def change(self,n,fn):
  row=self.manifest['iterations'][n-1];p=pathlib.Path(row['report']);raw=json.loads(p.read_text());fn(raw);stamp=p.stat().st_mtime;write(p,raw);os.utime(p,(stamp,stamp));row['sha256']=digest(p)
 def test_twenty_distinct_and_four_history_are_not_summed(self):
  r=self.validate();self.assertEqual(20,len(r['iterations']));self.assertEqual(4,len(r['history']));self.assertNotIn('total_cases',r);self.assertFalse(r['independent_execution_timestamps_verified']);self.assertEqual(20,r['final_iteration'])
 def test_only_nineteen_or_duplicated_phase_is_refused(self):
  for rows in (self.manifest['iterations'][:19],[self.manifest['iterations'][0]]*20):
   old=self.manifest['iterations'];self.manifest['iterations']=rows
   with self.assertRaises(RuntimeError):self.validate()
   self.manifest['iterations']=old
 def test_old_world_audio_success_cannot_stand_in_for_phase(self):
  self.change(10,lambda r:r.update(mode='world-audio'));self.manifest['iterations'][9]['mode']='world-audio'
  with self.assertRaises(RuntimeError):self.validate()
 def test_duplicate_run_identity_or_report_bytes_refused(self):
  self.change(2,lambda r:r.update(runId='fixture-1'));self.manifest['iterations'][1]['runId']='fixture-1'
  with self.assertRaises(RuntimeError):self.validate()
 def test_all_raw_error_and_incomplete_flags_refused(self):
  original=pathlib.Path(self.manifest['iterations'][0]['report']).read_bytes()
  for key,val in [('complete',False),('errorsFinalized',False),('failures',1),('unexpectedErrors',1),('fatal','exception'),('cases',999),('cannotVerify','')]:
   p=pathlib.Path(self.manifest['iterations'][0]['report']);stamp=p.stat().st_mtime;p.write_bytes(original);os.utime(p,(stamp,stamp));self.change(1,lambda r:r.update({key:val}))
   with self.subTest(key=key),self.assertRaises(RuntimeError):self.validate()
 def test_wrong_or_duplicate_check_even_with_same_count_refused(self):
  self.change(5,lambda r:r.update(audit=['PASS ordinary_player_and_native_zone']*2))
  with self.assertRaises(RuntimeError):self.validate()
 def test_missing_extra_and_renamed_capture_refused(self):
  self.change(3,lambda r:r['screenshots'].pop())
  with self.assertRaises(RuntimeError):self.validate()
 def test_image_content_hash_is_independent_of_report_hash(self):
  row=self.manifest['iterations'][0]['screenshots'][0];pathlib.Path(row['path']).write_bytes(b'changed')
  with self.assertRaises(RuntimeError):self.validate()
 def test_report_hash_is_required_even_if_summary_passes(self):
  self.manifest['iterations'][0]['sha256']='0'*64
  with self.assertRaises(RuntimeError):self.validate()
 def test_route_contract_and_source_hashes_are_pinned(self):
  src=self.root/'native-source.cs';src.write_text('// changed')
  with self.assertRaises(RuntimeError):self.validate()
 def test_unapproved_contract_bytes_rejected(self):
  write(self.file,self.manifest)
  with self.assertRaises(RuntimeError):self.p.validate_iterations(self.file,self.final,self.contract,'0'*64)
 def test_fresh_estimates_require_finite_duration_and_sequence(self):
  for value in [float('nan'),float('inf'),0,-1,True,90]:
   self.change(2,lambda r:r.update(seconds=value))
   with self.subTest(value=value),self.assertRaises(RuntimeError):self.validate()
 def test_estimate_cannot_be_presented_as_logged_execution(self):
  self.manifest['iterations'][0]['timeBasis']='native-utc'
  with self.assertRaises(RuntimeError):self.validate()
 def test_final_receipt_must_be_exact_phase20_bytes(self):
  self.final=pathlib.Path(self.manifest['iterations'][0]['report'])
  with self.assertRaises(RuntimeError):self.validate()
 def test_prior_four_cannot_disappear_or_count_as_new(self):
  self.manifest['history']=self.manifest['history'][:3]
  with self.assertRaises(RuntimeError):self.validate()
 def test_report_alias_refused(self):
  old=pathlib.Path(self.manifest['iterations'][0]['report']);alias=self.root/'alias.json';alias.symlink_to(old);self.manifest['iterations'][0]['report']=str(alias)
  with self.assertRaises(RuntimeError):self.validate()
 def test_missing_png_signature_refused_even_with_updated_hash(self):
  row=self.manifest['iterations'][0]['screenshots'][0];path=pathlib.Path(row['path']);path.write_bytes(b'text is not a screen');row['sha256']=digest(path)
  with self.assertRaises(RuntimeError):self.validate()
class Assets(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.p=load()
 def setUp(self):
  self.tmp=tempfile.TemporaryDirectory(prefix='coo-polish20-assets-');self.addCleanup(self.tmp.cleanup);self.repo=pathlib.Path(self.tmp.name);self.world=self.repo/'Assets/Resources/Audio/World';self.world.mkdir(parents=True);self.index=dict(schemaVersion=1,scope='audio-polish-final-assets',palettes=[])
  for i,label in enumerate(['original','places','atmosphere','utility','regional'],1):
   music=label=='regional';name=f'{label}/clip.ogg';clip=self.world/name;clip.parent.mkdir();clip.write_bytes(b'OggS fixture bytes '+label.encode());meta=clip.with_suffix('.ogg.meta');meta.write_text(f'guid: {i:032x}\nloadType: {2 if music else 0}\npreloadAudioData: {0 if music else 1}\nloadInBackground: {1 if music else 0}\ncompressionFormat: 1\nnormalize: 0\n')
   row=dict(id=label,path=name,kind='music' if music else 'sfx',source='source.ogg',author='Fixture Author',title='Fixture',page='https://example.invalid/source',license='CC0-1.0',licenseUrl='https://creativecommons.org/publicdomain/zero/1.0/',credit='Full fixture credit',changes='Fixture only, no listening claim',**(dict(context='organic',runtimeGain=.3) if music else dict(cue='Use',gain=.3)))
   src=dict(url='https://example.invalid/source',bytes=100,sha256='1'*64);spec=dict(schemaVersion=1,sources={'source.ogg':src},clips=[row]);prov=dict(schemaVersion=1,sources=spec['sources'],clips=[dict(row,outputSha256=digest(clip),outputBytes=clip.stat().st_size,sourceDecodedFrames=100,preparedFrames=100)])
   cat=dict(schemaVersion=1,music=[],ambience=[],sounds=[],details=[]);cat['music' if music else 'sounds'].append(dict(id='organic' if music else 'Use',resource='Audio/World/'+name[:-4],gain=.3));rec=dict(passed=True,assetCount=1,audioBytes=clip.stat().st_size,shortDecodedFloatBytes=0 if music else 400,findings=[],assets=[dict(path=name,errors=[])])

   if label=='original':rec['sfxDecodedFloatBytes']=rec.pop('shortDecodedFloatBytes')
   paths={key:write(self.repo/f'{label}-{key}.json',obj) for key,obj in [('spec',spec),('provenance',prov),('catalog',cat),('receipt',rec)]};cr=self.repo/f'{label}-credits.txt';cr.write_text('Full fixture credit\nhttps://example.invalid/source\nhttps://creativecommons.org/publicdomain/zero/1.0/\nContent ID; video description; music volume/mute\n');paths['credits']=cr
   self.index['palettes'].append(dict(id=label,**{k:dict(path=str(v.relative_to(self.repo)),sha256=digest(v)) for k,v in paths.items()}))
  lic=self.world/'Licenses';lic.mkdir()
  for name in self.p.LICENSE_FILES:(lic/name).write_bytes((pathlib.Path('/Users/steven/caves-of-ooo/Assets/Resources/Audio/World/Licenses')/name).read_bytes())
  self.path=self.repo/'asset-index.json'
 def validate(self):write(self.path,self.index);return self.p.validate_audio(self.repo,self.path,digest(self.path))
 def test_union_derived_from_manifests_and_no_hardcoded_final_count(self):
  r=self.validate();self.assertEqual(5,r['asset_count']);self.assertEqual(5,len(r['palettes']));self.assertFalse(r['subjective_listening_verified'])
 def test_extra_or_missing_clip_rejected(self):
  p=self.world/'utility/extra.ogg';p.write_bytes(b'OggS')
  with self.assertRaises(RuntimeError):self.validate()
  p.unlink();p=self.world/'utility/clip.ogg';p.unlink()
  with self.assertRaises(RuntimeError):self.validate()
 def test_independent_clip_provenance_hash_detects_mutation(self):
  (self.world/'utility/clip.ogg').write_bytes(b'OggS changed')
  with self.assertRaises(RuntimeError):self.validate()
 def test_missing_palette_or_duplicate_spec_rejected(self):
  self.index['palettes'][-1]=copy.deepcopy(self.index['palettes'][0])
  with self.assertRaises(RuntimeError):self.validate()
 def test_mutated_pinned_credits_catalog_or_spec_refused(self):
  for key in ['credits','catalog','spec','receipt','provenance']:
   path=self.repo/self.index['palettes'][0][key]['path'];old=path.read_bytes();path.write_bytes(old+b' ')
   with self.subTest(key=key),self.assertRaises(RuntimeError):self.validate()
   path.write_bytes(old)
 def test_short_import_policy_and_music_streaming_are_distinct(self):
  p=self.world/'regional/clip.ogg.meta';p.write_text(p.read_text().replace('loadType: 2','loadType: 0'))
  with self.assertRaises(RuntimeError):self.validate()
 def test_duplicate_guid_rejected(self):
  p=self.world/'regional/clip.ogg.meta';p.write_text(p.read_text().replace(f'{5:032x}',f'{1:032x}'))
  with self.assertRaises(RuntimeError):self.validate()
 def test_source_license_mutation_refused(self):
  p=self.world/'Licenses/CC0-1.0.txt';p.write_bytes(p.read_bytes()[:-1])
  with self.assertRaises(RuntimeError):self.validate()
 def repin(self,label,key,fn):
  item=next(p for p in self.index['palettes'] if p['id']==label);path=self.repo/item[key]['path'];obj=json.loads(path.read_text());fn(obj);write(path,obj);item[key]['sha256']=digest(path)
 def test_rehashed_bad_asset_count_and_individual_outcomes_refused(self):
  self.repin('utility','receipt',lambda r:r.update(assetCount=True))
  with self.assertRaises(RuntimeError):self.validate()
 def test_rehashed_cut_music_provenance_refused(self):
  self.repin('regional','provenance',lambda r:r['clips'][0].update(preparedFrames=99))
  with self.assertRaises(RuntimeError):self.validate()
 def test_rehashed_catalog_cannot_invent_a_gain_or_cue(self):
  self.repin('utility','catalog',lambda r:r['sounds'][0].update(gain=.7))
  with self.assertRaises(RuntimeError):self.validate()
 def test_rehashed_per_asset_error_refused(self):
  self.repin('utility','receipt',lambda r:r['assets'][0].update(errors=['decoder failed']))
  with self.assertRaises(RuntimeError):self.validate()
 def test_bound_source_file_cannot_escape_repository(self):
  temp=tempfile.NamedTemporaryFile(delete=False);temp.write(b'outside');temp.close();path=pathlib.Path(temp.name);self.addCleanup(path.unlink)
  self.index['palettes'][0]['credits']=dict(path=str(path),sha256=digest(path))
  with self.assertRaises(RuntimeError):self.validate()
 def test_asset_index_must_have_approved_hash(self):
  write(self.path,self.index)
  with self.assertRaises(RuntimeError):self.p.validate_audio(self.repo,self.path,'0'*64)
if __name__=='__main__':unittest.main(verbosity=2)
