"""Independent malformed-data and actual-file countercases for the scene verifier."""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest

import numpy as np
from PIL import Image

try:
    from . import verify_catacomb_scene as verifier
except ImportError:
    import verify_catacomb_scene as verifier


def fixture():
    asset = lambda name: {'path':name+'.png','sha256':'a'*64}
    return {'schemaVersion':1,'id':'test-scene','title':'Test scene',
        'canvas':{'width':8,'height':8,'pixelsPerCell':2},
        'base':asset('base'),'baseline':asset('baseline'),'underlay':asset('underlay'),
        'inferenceMask':asset('inference'),
        'navigation':{'width':4,'height':4,'spawn':[0,0],
            'walkableCells':[[x,y] for x in range(4) for y in range(4)],'waypoints':[]},
        'components':[{'id':'test-prop','name':'Test prop','sourceAssetId':'source-prop',
            'bounds':[2,2,2,2],'foot':[3,4],'pivot':[.5,0], 'depth':4,
            'sprite':asset('prop'),'contact':None,'mask':asset('mask'),
            'mutable':True,'defaultVisible':True,'blocksMovement':True,'footprintCells':[[1,1]]}]}


class ManifestAdversarialTests(unittest.TestCase):
    def test_valid_fixture_and_immutable_input(self):
        data=fixture();before=copy.deepcopy(data)
        self.assertTrue(verifier.validate_scene(data,{'assets':[{'id':'source-prop'}]})['ok'])
        self.assertEqual(data,before)

    def assert_bad(self,change):
        data=fixture();change(data)
        with self.assertRaises(ValueError):verifier.validate_scene(data,{'assets':[{'id':'source-prop'}]})

    def test_unsupported_schema(self):self.assert_bad(lambda d:d.update(schemaVersion=2))
    def test_boolean_schema(self):self.assert_bad(lambda d:d.update(schemaVersion=True))
    def test_empty_title(self):self.assert_bad(lambda d:d.update(title=' '))
    def test_duplicate_instance(self):self.assert_bad(lambda d:d['components'].append(copy.deepcopy(d['components'][0])))
    def test_unknown_source_asset(self):self.assert_bad(lambda d:d['components'][0].update(sourceAssetId='missing'))
    def test_duplicate_catalog_source(self):
        with self.assertRaises(ValueError):verifier.validate_scene(fixture(),{'assets':[{'id':'source-prop'},{'id':'source-prop'}]})
    def test_boolean_canvas_dimension(self):self.assert_bad(lambda d:d['canvas'].update(width=True))
    def test_fractional_bounds(self):self.assert_bad(lambda d:d['components'][0].update(bounds=[2,2,2.1,2]))
    def test_outside_bounds(self):self.assert_bad(lambda d:d['components'][0].update(bounds=[7,2,2,2]))
    def test_nonfinite_depth(self):self.assert_bad(lambda d:d['components'][0].update(depth=float('nan')))
    def test_foot_pivot_disagreement(self):self.assert_bad(lambda d:d['components'][0].update(pivot=[0,0]))
    def test_boolean_flag(self):self.assert_bad(lambda d:d['components'][0].update(mutable='true'))
    def test_duplicate_footprint(self):self.assert_bad(lambda d:d['components'][0].update(footprintCells=[[1,1],[1,1]]))
    def test_outside_footprint(self):self.assert_bad(lambda d:d['components'][0].update(footprintCells=[[4,1]]))
    def test_missing_blocker_footprint(self):self.assert_bad(lambda d:d['components'][0].update(footprintCells=[]))
    def test_blocked_spawn(self):self.assert_bad(lambda d:d['navigation'].update(spawn=[1,1]))
    def test_spawn_outside_terrain(self):self.assert_bad(lambda d:d['navigation']['walkableCells'].remove([0,0]))
    def test_duplicate_navigation_cell(self):self.assert_bad(lambda d:d['navigation']['walkableCells'].append([0,0]))
    def test_navigation_dimension_mismatch(self):self.assert_bad(lambda d:d['navigation'].update(width=3))
    def test_missing_hash(self):self.assert_bad(lambda d:d['base'].pop('sha256'))
    def test_missing_mutable_mask(self):self.assert_bad(lambda d:d['components'][0].update(mask=None))
    def test_unreachable_waypoint(self):self.assert_bad(lambda d:d['navigation'].update(waypoints=[{'id':'blocked','cell':[1,1]}]))


class FileAdversarialTests(unittest.TestCase):
    def test_unsafe_paths_fail_before_reading(self):
        with tempfile.TemporaryDirectory() as name:
            for value in ['../escape.png','/escape.png','a/../b.png','a//b.png','a/./b.png',
                r'a\b.png','https://host/a.png','%2e%2e/escape.png','a%20b.png',' file.png','a\x00.png','']:
                with self.subTest(path=value),self.assertRaises(ValueError):verifier.confined(Path(name),value)

    def test_symlink_escape_is_rejected_but_inside_is_allowed(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name)/'build';root.mkdir();outside=Path(name)/'outside';outside.write_text('x')
            (root/'escape').symlink_to(outside)
            with self.assertRaises(ValueError):verifier.confined(root,'escape')
            (root/'inside').write_text('y');(root/'alias').symlink_to(root/'inside')
            self.assertEqual(verifier.confined(root,'alias'),(root/'inside').resolve())

    def test_json_duplicate_keys_and_nonfinite_values_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            p=Path(name)/'data.json'
            for data in ['{"id":1,"id":2}','{"x":NaN}','{"x":Infinity}']:
                p.write_text(data)
                with self.subTest(data=data),self.assertRaises(ValueError):verifier.load_json(p)

    def make_png(self,root,mode='RGBA',alpha=(0,255,255,0)):
        path=root/'test.png'
        if mode=='RGBA':
            data=np.full((2,2,4),80,dtype=np.uint8);data[:,:,3]=np.array(alpha,dtype=np.uint8).reshape(2,2)
            Image.fromarray(data,'RGBA').save(path)
        else:Image.new(mode,(2,2)).save(path)
        return {'path':path.name,'sha256':hashlib.sha256(path.read_bytes()).hexdigest()}

    def test_real_png_alpha_and_hash_are_checked(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);record=self.make_png(root)
            self.assertEqual(verifier.read_png(root,record,[2,2],role='sprite').shape,(2,2,4))
            record['sha256']='0'*64
            with self.assertRaisesRegex(ValueError,'hash'):verifier.read_png(root,record,[2,2],role='sprite')

    def test_partial_alpha_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);record=self.make_png(root,alpha=(0,128,255,0))
            with self.assertRaisesRegex(ValueError,'binary'):verifier.read_png(root,record,[2,2],role='sprite')

    def test_opaque_rectangle_rejected_for_sprite(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);record=self.make_png(root,alpha=(255,255,255,255))
            with self.assertRaisesRegex(ValueError,'transparent'):verifier.read_png(root,record,[2,2],role='sprite')
            self.assertEqual(verifier.read_png(root,record,[2,2],role='opaque').shape,(2,2,4))

    def test_empty_sprite_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);record=self.make_png(root,alpha=(0,0,0,0))
            with self.assertRaisesRegex(ValueError,'opaque'):verifier.read_png(root,record,[2,2],role='sprite')

    def test_mismatched_dimensions_or_rgb_mode_rejected(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);record=self.make_png(root)
            with self.assertRaisesRegex(ValueError,'dimensions'):verifier.read_png(root,record,[3,2],role='sprite')
            record=self.make_png(root,mode='RGB')
            with self.assertRaisesRegex(ValueError,'RGBA'):verifier.read_png(root,record,[2,2],role='sprite')

    def test_missing_actual_asset_is_not_a_success(self):
        with tempfile.TemporaryDirectory() as name:
            with self.assertRaises((ValueError,OSError)):verifier.read_png(Path(name),{'path':'missing.png','sha256':'a'*64},[2,2],role='sprite')


class SourceDerivationTests(unittest.TestCase):
    def make_source(self,root):
        assets=root/'source-assets';(assets/'sprites').mkdir(parents=True)
        rgba=np.zeros((4,4,4),dtype=np.uint8);rgba[1:3,1:3]=[51,82,39,250]
        source=root/'input.png';Image.fromarray(rgba,'RGBA').save(source)
        (assets/'source.png').write_bytes(source.read_bytes())
        extractor=root/'extract.py';extractor.write_text('source extractor fixture\n')
        authoring={'assets':[{'id':'source-prop','bounds':[0,0,4,4],'foot':[2,3],'alphaThreshold':128}]}
        (root/'source-authoring.json').write_text(json.dumps(authoring))
        sprite=rgba.copy();sprite[:,:,3]=(sprite[:,:,3]>=128)*255
        path=assets/'sprites/prop.png';Image.fromarray(sprite,'RGBA').save(path)
        catalog={'schemaVersion':1,'source':{'path':'source.png','sha256':verifier.sha256(source),'width':4,'height':4},
            'authoringSha256':verifier.sha256(root/'source-authoring.json'),'extractorSha256':verifier.sha256(extractor),
            'assets':[{'id':'source-prop','path':'sprites/prop.png','spriteSha256':verifier.sha256(path),
                'sourceSha256':verifier.sha256(source),'sourceBounds':[0,0,4,4],'sourceRoi':[0,0,4,4],
                'width':4,'height':4,'foot':[2,3],'pivot':[.5,.25],'alphaThreshold':128}]}
        (assets/'catalog.json').write_text(json.dumps(catalog))
        return source,extractor,catalog

    def check(self,root,source,extractor):return verifier.source_derivation(root,source,extractor)

    def test_independent_source_pixels_and_alpha_match(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,_=self.make_source(root)
            self.assertEqual(self.check(root,source,extractor)['sourceAssetCount'],1)

    def test_rehashed_changed_visible_pixel_is_not_derived(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,catalog=self.make_source(root);p=root/'source-assets/sprites/prop.png'
            image=np.asarray(Image.open(p)).copy();image[1,1,0]+=1;Image.fromarray(image,'RGBA').save(p)
            catalog['assets'][0]['spriteSha256']=verifier.sha256(p);(root/'source-assets/catalog.json').write_text(json.dumps(catalog))
            with self.assertRaisesRegex(ValueError,'deriv'):self.check(root,source,extractor)

    def test_rehashed_extra_alpha_pixel_is_not_derived(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,catalog=self.make_source(root);p=root/'source-assets/sprites/prop.png'
            image=np.asarray(Image.open(p)).copy();image[0,0]=[51,82,39,255];Image.fromarray(image,'RGBA').save(p)
            catalog['assets'][0]['spriteSha256']=verifier.sha256(p);(root/'source-assets/catalog.json').write_text(json.dumps(catalog))
            with self.assertRaisesRegex(ValueError,'deriv'):self.check(root,source,extractor)

    def test_stale_extractor_fails_even_with_same_assets(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,_=self.make_source(root);extractor.write_text('changed code\n')
            with self.assertRaisesRegex(ValueError,'extractor'):self.check(root,source,extractor)

    def test_stale_authoring_fails_even_with_same_assets(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,_=self.make_source(root);p=root/'source-authoring.json';p.write_text(p.read_text()+'\n')
            with self.assertRaisesRegex(ValueError,'authoring'):self.check(root,source,extractor)

    def test_forged_source_asset_id_fails(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);source,extractor,catalog=self.make_source(root);catalog['assets'][0]['id']='forged'
            (root/'source-assets/catalog.json').write_text(json.dumps(catalog))
            with self.assertRaisesRegex(ValueError,'identit'):self.check(root,source,extractor)


class AtlasTests(unittest.TestCase):
    def make_atlas(self,root):
        data=fixture();sprite=np.array([[[0,0,0,0],[10,20,30,255]],[[20,30,40,255],[0,0,0,0]]],dtype=np.uint8)
        p=root/'prop.png';Image.fromarray(sprite,'RGBA').save(p);data['components'][0]['sprite']['sha256']=verifier.sha256(p)
        (root/'scene.json').write_text(json.dumps(data))
        image=np.zeros((4,4,4),np.uint8);image[1:3,1:3]=sprite
        p=root/'prop-atlas.png';Image.fromarray(image,'RGBA').save(p)
        atlas={'schemaVersion':1,'path':p.name,'sha256':verifier.sha256(p),'size':[4,4],
            'entries':[{'id':'test-prop','sourceRectTopLeft':[1,1,2,2],'unityRectBottomLeft':[1,1,2,2],
                'pivot':[.5,0],'sourceSpriteSha256':data['components'][0]['sprite']['sha256']}]}
        (root/'prop-atlas.json').write_text(json.dumps(atlas));return atlas

    def test_independent_exact_atlas(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);self.make_atlas(root);self.assertEqual(verifier.atlas_check(root)['spriteCount'],1)

    def bad_metadata(self,change):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);atlas=self.make_atlas(root);change(atlas);(root/'prop-atlas.json').write_text(json.dumps(atlas))
            with self.assertRaises(ValueError):verifier.atlas_check(root)

    def test_duplicate_atlas_id(self):self.bad_metadata(lambda a:a['entries'].append(copy.deepcopy(a['entries'][0])))
    def test_missing_atlas_id(self):self.bad_metadata(lambda a:a.update(entries=[]))
    def test_unknown_atlas_id(self):self.bad_metadata(lambda a:a['entries'][0].update(id='unknown'))
    def test_outside_atlas_rect(self):self.bad_metadata(lambda a:a['entries'][0].update(sourceRectTopLeft=[3,1,2,2]))
    def test_wrong_bottom_left_y(self):self.bad_metadata(lambda a:a['entries'][0].update(unityRectBottomLeft=[1,2,2,2]))
    def test_wrong_atlas_pivot(self):self.bad_metadata(lambda a:a['entries'][0].update(pivot=[0,0]))

    def test_rehashed_changed_atlas_pixels_fail(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);atlas=self.make_atlas(root);p=root/atlas['path'];image=np.asarray(Image.open(p)).copy()
            image[1,2,0]+=1;Image.fromarray(image,'RGBA').save(p);atlas['sha256']=verifier.sha256(p)
            (root/'prop-atlas.json').write_text(json.dumps(atlas))
            with self.assertRaisesRegex(ValueError,'pixels'):verifier.atlas_check(root)

    def test_undeclared_atlas_pixel_fails(self):
        with tempfile.TemporaryDirectory() as name:
            root=Path(name);atlas=self.make_atlas(root);p=root/atlas['path'];image=np.asarray(Image.open(p)).copy()
            image[0,0]=[1,2,3,255];Image.fromarray(image,'RGBA').save(p);atlas['sha256']=verifier.sha256(p)
            (root/'prop-atlas.json').write_text(json.dumps(atlas))
            with self.assertRaisesRegex(ValueError,'undeclared'):verifier.atlas_check(root)


class PlacedDerivationTests(unittest.TestCase):
    def make_placed(self,root):
        sources=root/'sources';sources.mkdir();build=root/'build';build.mkdir()
        sprite=np.array([[[0,0,0,0],[10,20,30,255]],[[20,30,40,255],[0,0,0,0]]],dtype=np.uint8)
        Image.fromarray(sprite,'RGBA').save(sources/'source.png')
        Image.fromarray(sprite,'RGBA').save(build/'prop.png')
        digest=verifier.sha256(sources/'source.png')
        catalog={'assets':[{'id':'source-prop','path':'source.png','spriteSha256':digest,'width':2,'height':2}]}
        (sources/'catalog.json').write_text(json.dumps(catalog));data=fixture()
        data['components'][0].update(sourceSpriteSha256=digest,scale=1,resize='nearest',sourceSize=[2,2],placedSize=[2,2])
        data['components'][0]['sprite']['sha256']=verifier.sha256(build/'prop.png')
        (build/'scene.json').write_text(json.dumps(data));return build,sources,data

    def test_placed_source_exact(self):
        with tempfile.TemporaryDirectory() as name:
            build,sources,_=self.make_placed(Path(name))
            self.assertEqual(verifier.placed_derivation(build,sources)['componentCount'],1)

    def test_rehashed_art_change_is_not_source_resize(self):
        with tempfile.TemporaryDirectory() as name:
            build,sources,data=self.make_placed(Path(name));p=build/'prop.png';image=np.asarray(Image.open(p)).copy()
            image[1,0,0]+=1;Image.fromarray(image,'RGBA').save(p);data['components'][0]['sprite']['sha256']=verifier.sha256(p)
            (build/'scene.json').write_text(json.dumps(data))
            with self.assertRaisesRegex(ValueError,'deriv'):verifier.placed_derivation(build,sources)

    def bad_placed(self,change):
        with tempfile.TemporaryDirectory() as name:
            build,sources,data=self.make_placed(Path(name));change(data['components'][0]);(build/'scene.json').write_text(json.dumps(data))
            with self.assertRaises(ValueError):verifier.placed_derivation(build,sources)

    def test_missing_source_resize_record(self):self.bad_placed(lambda c:c.pop('resize'))
    def test_wrong_source_resize_scale(self):self.bad_placed(lambda c:c.update(scale=2))
    def test_wrong_source_sprite_hash(self):self.bad_placed(lambda c:c.update(sourceSpriteSha256='0'*64))
    def test_invalid_source_scale(self):self.bad_placed(lambda c:c.update(scale=float('inf')))


if __name__=='__main__':unittest.main()
