"""Lossless destructible voxel interchange independently of render meshing."""
import unittest
from town_generator.mesh import VoxelMesh
from town_generator.scene import voxel_runs

class ExportTests(unittest.TestCase):
    def decode(self,runs):
        cells={}
        for x,y,z,length,material in runs:
            self.assertGreater(length,0)
            for xx in range(x,x+length):
                self.assertNotIn((xx,y,z),cells)
                cells[xx,y,z]=material
        return cells
    def test_lossless_irregular_negative_occupancy(self):
        m=VoxelMesh();m.bounds((-2,-1,-.5),(1,2,.5),'wood');m.bounds((0,0,0),(2,1,2),'stone_light')
        self.assertEqual(self.decode(voxel_runs(m)),m.cells)
    def test_empty_has_no_phantom_voxels(self):self.assertEqual(voxel_runs(VoxelMesh()),[])
    def test_repainted_region_retains_material(self):
        m=VoxelMesh();m.bounds((0,0,0),(2,1,1),'wood');m.bounds((.5,0,0),(1,1,1),'metal')
        self.assertEqual(self.decode(voxel_runs(m)),m.cells)
        self.assertEqual(len(set(row[-1] for row in voxel_runs(m))),2)
    def test_order_does_not_depend_on_box_authoring_sequence(self):
        a=VoxelMesh();b=VoxelMesh()
        boxes=[((-1,0,0),(0,1,1)),((1,0,0),(2,1,1))]
        for lo,hi in boxes:a.bounds(lo,hi,'wood')
        for lo,hi in reversed(boxes):b.bounds(lo,hi,'wood')
        self.assertEqual(voxel_runs(a),voxel_runs(b))
    def test_gap_remains_destructible_air(self):
        m=VoxelMesh();m.bounds((0,0,0),(.25,.25,.25),'wood');m.bounds((.5,0,0),(.75,.25,.25),'wood')
        cells=self.decode(voxel_runs(m));self.assertNotIn((1,0,0),cells);self.assertIn((2,0,0),cells)
    def test_runs_remain_separate_on_other_axes(self):
        m=VoxelMesh();m.bounds((0,0,0),(1,1,1),'wood')
        runs=voxel_runs(m);self.assertEqual(len(runs),16);self.assertEqual(self.decode(runs),m.cells)
