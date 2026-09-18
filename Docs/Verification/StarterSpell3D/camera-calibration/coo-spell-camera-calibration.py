import bpy, json, math
from mathutils import Vector
from bpy_extras.object_utils import world_to_camera_view

bpy.ops.wm.read_factory_settings(use_empty=True)
scene=bpy.context.scene
camera_data=bpy.data.cameras.new('CalibrationCamera')
camera=bpy.data.objects.new('CalibrationCamera', camera_data)
scene.collection.objects.link(camera)
scene.camera=camera
camera_data.type='ORTHO'
camera_data.ortho_scale=16.0
camera_data.clip_start=0.01
camera_data.clip_end=200
theta=math.radians(56)
height=35.0
camera.location=(0, -height/math.tan(theta), height)
camera.rotation_euler=(math.pi/2-theta, 0, 0)
scene.render.resolution_percentage=100
rows=[]
for resolution in [(1600,900),(960,960),(900,1600),(1800,600)]:
  scene.render.resolution_x, scene.render.resolution_y=resolution
  for fit in ['AUTO','HORIZONTAL','VERTICAL']:
    camera_data.sensor_fit=fit
    for label, px, py in [('square',1,1),('aspect_y_sin',1,math.sin(theta)),('aspect_y_inverse',1,1/math.sin(theta)),('aspect_x_inverse',1/math.sin(theta),1)]:
      scene.render.pixel_aspect_x=px
      scene.render.pixel_aspect_y=py
      actual_px=scene.render.pixel_aspect_x
      actual_py=scene.render.pixel_aspect_y
      bpy.context.view_layer.update()
      graph=bpy.context.evaluated_depsgraph_get()
      evaluated=camera.evaluated_get(graph)
      matrix=evaluated.calc_matrix_camera(graph,x=resolution[0],y=resolution[1],scale_x=actual_px,scale_y=actual_py)
      points=[Vector((0,0,0)),Vector((1,0,0)),Vector((0,1,0)),Vector((0,0,1))]
      ndc=[world_to_camera_view(scene,camera,p) for p in points]
      projected=[]
      view=camera.matrix_world.inverted()
      for p in points:
        q=matrix@view@p.to_4d()
        projected.append(((q.x/q.w+1)*.5,(q.y/q.w+1)*.5))
      spanx=(ndc[1].x-ndc[0].x)*resolution[0]
      spany=(ndc[2].y-ndc[0].y)*resolution[1]
      raised=(ndc[3].y-ndc[0].y)*resolution[1]
      rows.append(dict(resolution=resolution,sensor_fit=fit,variant=label,requested_pixel_aspect=[px,py],actual_pixel_aspect=[actual_px,actual_py],unit_x_pixels=spanx,unit_y_pixels=spany,equal_ground_error=spany/spanx-1,height_shift_ground_units=raised/spany,height_shift_expected=1/math.tan(theta),helper_matrix_max_error=max(abs(ndc[i][j]-projected[i][j]) for i in range(4) for j in range(2)),projection_diagonal=[matrix[0][0],matrix[1][1]]))
output='/tmp/coo-spell-camera-calibration.json'
with open(output,'w') as f:json.dump(dict(blender=bpy.app.version_string,pitch_degrees=56,rows=rows),f,indent=2)
print('COO_CAMERA_CALIBRATION', output)
for row in rows:
  if row['sensor_fit']=='AUTO':print(row)
checks=[]
for row in rows:
  if row['sensor_fit']!='HORIZONTAL' or row['variant']!='aspect_x_inverse':continue
  assert abs(row['equal_ground_error'])<2e-6, row
  assert abs(row['height_shift_ground_units']-row['height_shift_expected'])<2e-6, row
  assert row['helper_matrix_max_error']<1e-6, row
  assert abs(row['unit_x_pixels']-row['resolution'][0]/16)<.001, row
  checks.append(row['resolution'])
image=bpy.data.images.new('PixelAspectMetadataProbe',width=32,height=18,alpha=True)
scene.render.image_settings.file_format='PNG'
scene.render.image_settings.color_mode='RGBA'
scene.render.pixel_aspect_x=1/math.sin(theta)
scene.render.pixel_aspect_y=1
image.save_render('/tmp/coo-spell-camera-pixel-aspect-metadata.png',scene=scene)
print('COO_CAMERA_ASSERTIONS_PASS',len(checks)*4,checks)
