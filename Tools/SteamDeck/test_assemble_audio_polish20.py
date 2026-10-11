"""Synthetic assembly gates only: no Unity, decode, gameplay or packaging."""
import datetime, importlib.util, json, os, pathlib, struct, tempfile, unittest
import test_package_audio_polish20 as fixtures
HERE=pathlib.Path(__file__).parent

def load():
 s=importlib.util.spec_from_file_location('assemble20',HERE/'assemble-audio-polish20.py');m=importlib.util.module_from_spec(s);s.loader.exec_module(m);return m

def write(p,data):
 p.parent.mkdir(parents=True,exist_ok=True);p.write_text(json.dumps(data));return p

class Assembly(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.m=load();fixtures.Assets.setUpClass()
 def setUp(self):
  self.assets=fixtures.Assets('test_union_derived_from_manifests_and_no_hardcoded_final_count');self.assets.setUp();self.addCleanup(self.assets.doCleanups)
  self.repo=self.assets.repo;self.output=self.repo/'Docs/delivery';self.selection=self.repo/'selection.json'
  native=self.repo/'Assets/Scripts/Scenarios/Custom';native.mkdir(parents=True)
  common=native/'ReferenceGladeNativePlayer.cs';common.write_text('Check("ordinary_player_and_native_zone",true); yield return Capture("01-gameplay-arrival"); if(_audioPolishIteration>0){yield return RunAudioPolishAudit();yield break;}')
  dispatcher=native/'ReferenceGladeNativePlayer.AudioPolish.cs';dispatcher.write_text('\n'.join(f'case {n}: yield return RunAudioPolish{n:02}(); break; // route\n var c{n}=AudioPolish{n:02}Checks;' for n in range(1,21)))
  self.data=dict(schemaVersion=1,scope='audio-polish-evidence-selection',notBeforeUtc='2026-10-11T00:00:00Z',commonSource=str(common),dispatcherSource=str(dispatcher),phases=[],history=[],palettes=[])
  for n in range(1,25):
   historical=n>20;i=n-20 if historical else n;mode=('native-work-audio' if i==3 else 'world-audio') if historical else f'audio-polish-{i:02}'
   folder=self.repo/'Docs/Verification'/('history' if historical else 'live')/str(i);folder.mkdir(parents=True)
   shots=[]
   for name in ['01-gameplay-arrival.png',f'action-{i}.png']:
    p=folder/name;p.write_bytes(b'\x89PNG\r\n\x1a\n'+struct.pack('>I',13)+b'IHDR'+struct.pack('>II',1920,1080)+b'synthetic');shots.append(str(p))
   raw=dict(mode=mode,runId=f'synthetic-{n}',complete=True,errorsFinalized=True,failures=0,unexpectedErrors=0,fatal='',cases=2,audit=['PASS ordinary_player_and_native_zone',f'PASS phase{i}_actual_action'],seconds=5,screenshots=shots,canVerify='Synthetic only',cannotVerify='Not live evidence')
   p=write(folder/'report.json',raw);stamp=datetime.datetime(2026,10,11,0,i,tzinfo=datetime.timezone.utc).timestamp();os.utime(p,(stamp,stamp))
   row=dict(iteration=i,report=str(p))
   if not historical:
    source=native/f'ReferenceGladeNativePlayer.AudioPolish{i:02}.cs';source.write_text(f'static readonly string[] AudioPolish{i:02}Checks={{"phase{i}_actual_action"}}; IEnumerator RunAudioPolish{i:02}(){{yield return Capture("action-{i}");}}');row['source']=str(source)
   self.data['history' if historical else 'phases'].append(row)
  self.data['finalReport']=self.data['phases'][-1]['report']
  for p in self.assets.index['palettes']:
   self.data['palettes'].append(dict(id=p['id'],**{k:str(self.repo/v['path']) for k,v in p.items() if k!='id'}))
 def run_assembly(self):write(self.selection,self.data);return self.m.assemble(self.repo,self.selection,self.output)
 def raw_change(self,n,fn):
  p=pathlib.Path(self.data['phases'][n-1]['report']);r=json.loads(p.read_text());stamp=p.stat().st_mtime;fn(r);write(p,r);os.utime(p,(stamp,stamp))
 def test_complete_synthetic_selection_publishes_separate_history_and_exact_hashes(self):
  before={p:(p.read_bytes(),p.stat().st_mtime_ns) for p in self.repo.rglob('*') if p.is_file()};result=self.run_assembly()
  self.assertEqual(20,len(result['iterations']));self.assertEqual(4,len(result['history']));self.assertEqual(5,result['assets']['asset_count']);self.assertNotIn('total_cases',result)
  historical=json.loads((self.output/'history-manifest.json').read_text());self.assertEqual(4,len(historical['history']));self.assertFalse(result['independent_execution_timestamps_verified'])
  for p,(data,stamp) in before.items():self.assertEqual(data,p.read_bytes());self.assertEqual(stamp,p.stat().st_mtime_ns)
  for row in result['outputs'].values():self.assertEqual(row['sha256'],fixtures.digest(self.output/row['name']))
 def test_missing_phase_refuses_without_outputs(self):
  self.data['phases'].pop()
  with self.assertRaises(RuntimeError):self.run_assembly()
  self.assertFalse(self.output.exists())
 def test_failed_raw_report_refuses_without_outputs(self):
  self.raw_change(10,lambda r:r.update(complete=False,failures=1))
  with self.assertRaises(RuntimeError):self.run_assembly()
  self.assertFalse(self.output.exists())
 def test_duplicate_success_cannot_substitute(self):
  self.data['phases'][1]['report']=self.data['phases'][0]['report']
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_wrong_same_count_check_is_refused(self):
  self.raw_change(9,lambda r:r.update(audit=['PASS ordinary_player_and_native_zone','PASS invented_success']))
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_missing_history_is_not_filled_by_new_reports(self):
  self.data['history'].pop()
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_missing_palette_and_extra_actual_clip_are_refused(self):
  self.data['palettes'].pop()
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_failed_per_asset_result_refuses(self):
  p=pathlib.Path(self.data['palettes'][3]['receipt']);r=json.loads(p.read_text());r['assets'][0]['errors']=['failed'];write(p,r)
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_final_report_must_equal_selected_phase20(self):
  self.data['finalReport']=self.data['phases'][18]['report']
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_shared_or_dispatch_binding_absent_refuses(self):
  pathlib.Path(self.data['dispatcherSource']).write_text('// case 20: yield return RunAudioPolish20(); AudioPolish20Checks')
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_input_alias_and_outside_repository_refuse(self):
  original=pathlib.Path(self.data['phases'][0]['source']);link=self.repo/'alias.cs';link.symlink_to(original);self.data['phases'][0]['source']=str(link)
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_existing_output_and_assets_output_refuse(self):
  self.output.mkdir(parents=True);sentinel=self.output/'keep';sentinel.write_text('keep')
  with self.assertRaises(RuntimeError):self.run_assembly()
  self.assertEqual('keep',sentinel.read_text());self.output=self.repo/'Assets/no-write'
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_source_changed_capture_refuses_even_with_passing_raw(self):
  p=pathlib.Path(self.data['phases'][0]['source']);p.write_text(p.read_text().replace('action-1','different'))
  with self.assertRaises(RuntimeError):self.run_assembly()
 def test_a_document_copy_cannot_impersonate_final_repository_route(self):
  p=self.repo/'Docs/copied-route.cs';p.parent.mkdir(exist_ok=True);p.write_text(pathlib.Path(self.data['phases'][0]['source']).read_text());self.data['phases'][0]['source']=str(p)
  with self.assertRaisesRegex(RuntimeError,'canonical route source'):self.run_assembly()
 def test_no_timestamp_input_is_invented(self):
  self.data.pop('notBeforeUtc')
  with self.assertRaises(RuntimeError):self.run_assembly()

 def test_extra_actual_ogg_is_refused(self):
  (self.assets.world/'unapproved.ogg').write_bytes(b'OggS unapproved')
  with self.assertRaisesRegex(RuntimeError,'exact approved source-manifest union'):self.run_assembly()
 def test_boolean_native_count_is_not_one_pass(self):
  self.raw_change(1,lambda r:r.update(cases=True))
  with self.assertRaisesRegex(RuntimeError,'count'):self.run_assembly()
 def test_source_array_extra_check_requires_real_report_evidence(self):
  p=pathlib.Path(self.data['phases'][0]['source']);p.write_text(p.read_text().replace('{"phase1_actual_action"}','{"phase1_actual_action","new_check"}'))
  with self.assertRaisesRegex(RuntimeError,'Wrong phase checks'):self.run_assembly()
 def test_unrelated_extra_png_cannot_replace_route_capture(self):
  self.raw_change(2,lambda r:r['screenshots'].pop())
  with self.assertRaisesRegex(RuntimeError,'Wrong exact phase captures'):self.run_assembly()
 def test_report_mtime_order_remains_explicit_estimate(self):
  p=pathlib.Path(self.data['phases'][5]['report']);stamp=pathlib.Path(self.data['phases'][0]['report']).stat().st_mtime;os.utime(p,(stamp,stamp))
  with self.assertRaisesRegex(RuntimeError,'out-of-order'):self.run_assembly()
 def test_real_external_source_and_parent_alias_refused(self):
  with tempfile.TemporaryDirectory() as tmp:
   external=pathlib.Path(tmp)/'source.cs';external.write_text(pathlib.Path(self.data['phases'][0]['source']).read_text());self.data['phases'][0]['source']=str(external)
   with self.assertRaisesRegex(RuntimeError,'selected repository'):self.run_assembly()
  target=self.repo/'Docs';alias=self.repo/'alias-parent';alias.symlink_to(target,target_is_directory=True);self.output=alias/'new-output'
  with self.assertRaisesRegex(RuntimeError,'Symlink'):self.run_assembly()
 def test_existing_output_second_run_cannot_overwrite_validation(self):
  self.run_assembly();before={p.name:p.read_bytes() for p in self.output.iterdir()}
  with self.assertRaisesRegex(RuntimeError,'Output must be new'):self.run_assembly()
  self.assertEqual(before,{p.name:p.read_bytes() for p in self.output.iterdir()})

class Parser(unittest.TestCase):
 @classmethod
 def setUpClass(cls):cls.m=load()
 def test_exact_strings_ignore_comment_and_quoted_decoys(self):
  src='// Capture("wrong");\nstring text="Capture(\\"bad\\")"; static string[] AudioPolish01Checks={"real"}; yield return Capture("actual");'
  self.assertEqual((['real'],['actual.png']),self.m.phase_contract(src,1))
 def test_known_finite_regional_expression_derives_both_names(self):
  src='static string[] AudioPolish16Checks={"sodden","grovelands"}; foreach(var biome in new[]{BiomeType.Sodden,BiomeType.Grovelands}){ string id=biome==BiomeType.Sodden?"sodden":"grovelands"; yield return Capture("regional16-"+id+"-actual-ground"); }'
  self.assertEqual(['regional16-sodden-actual-ground.png','regional16-grovelands-actual-ground.png'],self.m.phase_contract(src,16)[1])
 def test_unknown_dynamic_expression_fails_closed(self):
  with self.assertRaises(RuntimeError):self.m.phase_contract('static string[] AudioPolish01Checks={"real"}; Capture(GetName());',1)
 def test_conditional_without_two_value_loop_is_not_assumed_complete(self):
  with self.assertRaises(RuntimeError):self.m.phase_contract('static string[] AudioPolish16Checks={"real"}; string id=biome==BiomeType.Sodden?"sodden":"grovelands"; Capture("region-"+id);',16)
 def test_duplicate_or_nonliteral_check_declarations_fail(self):
  for src in ['static string[] AudioPolish01Checks={"a","a"}; Capture("a");','static string[] AudioPolish01Checks={name}; Capture("a");']:
   with self.subTest(src=src),self.assertRaises(RuntimeError):self.m.phase_contract(src,1)
 def test_regional_name_reassignment_is_not_proven(self):
  src='static string[] AudioPolish16Checks={"sodden","grovelands"}; foreach(var biome in new[]{BiomeType.Sodden,BiomeType.Grovelands}){ string id=biome==BiomeType.Sodden?"sodden":"grovelands"; id+="changed"; yield return Capture("regional16-"+id+"-actual-ground"); }'
  with self.assertRaisesRegex(RuntimeError,'Unsupported/unproven'):self.m.phase_contract(src,16)

if __name__=='__main__':unittest.main(verbosity=2)
