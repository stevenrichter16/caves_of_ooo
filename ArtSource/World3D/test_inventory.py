"""Coverage must retain gaps, inheritance and source evidence honestly."""
import unittest
from inventory import resolve_blueprints, coverage_status, sprite_tables
class InventoryTests(unittest.TestCase):
 def test_parent_parameters_survive(self):
  rows=[{'Name':'P','Parts':[{'Name':'Render','Params':[{'Key':'Color','Value':'red'},{'Key':'Glyph','Value':'x'}]}]}, {'Name':'C','Inherits':'P','Parts':[{'Name':'Render','Params':[{'Key':'Glyph','Value':'y'}]}]}]
  self.assertEqual(resolve_blueprints(rows)['C']['parts']['Render'],{'Color':'red','Glyph':'y'})
 def test_child_does_not_mutate_parent(self):
  r=resolve_blueprints([{'Name':'P','Tags':[{'Key':'a','Value':'1'}]},{'Name':'C','Inherits':'P','Tags':[{'Key':'a','Value':'2'}]}]);self.assertEqual(r['P']['tags']['a'],'1')
 def test_out_of_order_parent(self):
  self.assertEqual(resolve_blueprints([{'Name':'C','Inherits':'P'},{'Name':'P'}])['C']['ancestors'],['P'])
 def test_cycle_rejected(self):
  with self.assertRaises(ValueError):resolve_blueprints([{'Name':'A','Inherits':'B'},{'Name':'B','Inherits':'A'}])
 def test_missing_parent_rejected(self):
  with self.assertRaises(ValueError):resolve_blueprints([{'Name':'A','Inherits':'absent'}])
 def test_duplicates_rejected(self):
  with self.assertRaises(ValueError):resolve_blueprints([{'Name':'A'},{'Name':'A'}])
 def test_missing_art_not_complete(self):self.assertEqual(coverage_status([],[]),'uncovered')
 def test_local_art_not_global(self):self.assertEqual(coverage_status(['ring'],[]),'local-only')
 def test_unreviewed_art_not_complete(self):self.assertEqual(coverage_status([],['candidate']),'candidate-only')
 def test_local_plus_candidate_not_complete(self):self.assertEqual(coverage_status(['ring'],['candidate']),'local-only+candidate')
 def test_mapping_ignores_comments(self):
  s='public static readonly (string Blueprint, string File)[] FixtureSprites = { // ("Wrong", "bad")\n ("Tree", "tree"), };'
  self.assertEqual(sprite_tables(s),{'Tree':['Sprites/Environment/tree']})
 def test_mapping_retains_shared_sprite_identities(self):
  s='public static readonly (string Blueprint, string File)[] FixtureSprites = { ("A", "same"), ("B", "same"), };'
  self.assertEqual(len(sprite_tables(s)),2)
if __name__=='__main__':unittest.main()
