"""Blender: --background input.blend --python export_basis.py -- --object Crate_low --out .../Crate.obj --texture IRME/Crate_BaseColor
Exports one evaluated, triangulated, static object, OBJ + MTL + SHA256-bound IMB1 basis.
Bake on this same triangulation first. No scene mutations and no external API calls.
"""
import bpy, sys, argparse, struct, hashlib, json
from pathlib import Path
from mathutils import Matrix

def export(obj, out, texture, tint=(1,1,1)):
 out=Path(out);out.parent.mkdir(parents=True,exist_ok=True)
 dg=bpy.context.evaluated_depsgraph_get();eo=obj.evaluated_get(dg);me=eo.to_mesh()
 try:
  if not me.uv_layers.active: raise ValueError('Active UV map required')
  if len({p.material_index for p in me.polygons})>1: raise ValueError('This exporter supports one material per object; split or atlas before baking')
  if any(len(p.vertices)!=3 for p in me.polygons): raise ValueError('Triangulate and bake before export; refusing to alter bake triangulation')
  if obj.matrix_world.to_3x3().determinant()<=0: raise ValueError('Apply mirror/negative scale before baking')
  # Blender z-up -> OBJ y-up -> IsoRim flips z. OBJ conversion has negative determinant;
  # its flipped winding is correct after the native loader flips z back.
  C=Matrix(((1,0,0),(0,0,1),(0,-1,0)))
  A=C@obj.matrix_world.to_3x3();N=A.inverted().transposed()
  me.calc_loop_triangles();me.calc_tangents(uvmap=me.uv_layers.active.name)
  lines=['# IRME export; triangulation and per-corner basis frozen',f'mtllib {out.stem}.mtl','o Asset','usemtl Crate']
  records=[];idx=0
  for tri in me.loop_triangles:
   face=[]
   for li in tri.loops:
    loop=me.loops[li];p=C@(obj.matrix_world@me.vertices[loop.vertex_index].co)
    n=(N@me.corner_normals[li].vector).normalized();t=A@loop.tangent;t=(t-n*n.dot(t)).normalized()
    uv=me.uv_layers.active.data[li].uv;w=loop.bitangent_sign
    # OBJ is the z-flipped representation of the actual game-space record.
    lines += [f'v {p.x:.9f} {p.y:.9f} {-p.z:.9f}',f'vt {uv.x:.9f} {uv.y:.9f}',f'vn {n.x:.9f} {n.y:.9f} {-n.z:.9f}']
    records.append((*p,*n,*uv,*t,w));idx+=1;face.append(f'{idx}/{idx}/{idx}')
   lines.append('f '+' '.join(face))
  raw=('\n'.join(lines)+'\n').encode();out.write_bytes(raw)
  out.with_suffix('.mtl').write_text(f'newmtl Crate\nKd {tint[0]} {tint[1]} {tint[2]}\nmap_Kd {texture}.png\n',encoding='utf-8')
  with open(str(out)+'.irbasis','wb') as f:
   f.write(struct.pack('<I',0x31424D49));f.write(hashlib.sha256(raw).digest());f.write(struct.pack('<i',len(records)))
   for rec in records:f.write(struct.pack('<12f',*rec))
  return {'object':obj.name,'triangles':len(records)//3,'corners':len(records),'material_slots_exported':1,'basis':'Blender MikkTSpace, game-space (x,z,-y)','file':str(out)}
 finally:eo.to_mesh_clear()

if __name__=='__main__':
 p=argparse.ArgumentParser();p.add_argument('--object',required=True);p.add_argument('--out',required=True);p.add_argument('--texture',required=True);p.add_argument('--tint',nargs=3,type=float,default=(1,1,1))
 a=p.parse_args(sys.argv[sys.argv.index('--')+1:]);print(json.dumps(export(bpy.data.objects[a.object],a.out,a.texture,a.tint)))
