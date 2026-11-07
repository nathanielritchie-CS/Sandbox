# The worn starter sword (spec 22): dull chipped blade with rust, plain bar guard, cloth-wrapped grip.
#   blender --background --factory-startup --python tools/blender/starter_sword.py -- [--out <file.fbx>] [--renders <dir>]
# Origin = the middle of the grip; the blade points up (+Z here, +Y in Unity).
import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Vector

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(name, default=None):
    return ARGV[ARGV.index(name) + 1] if name in ARGV else default


OUT, RENDERS = arg("--out"), arg("--renders")
random.seed(7)
bpy.ops.wm.read_factory_settings(use_empty=True)


def srgb(c):
    c /= 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


def material(name, rgb, rough, metal):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    col = tuple(srgb(c) for c in rgb) + (1.0,)
    bsdf.inputs["Base Color"].default_value = col
    bsdf.inputs["Roughness"].default_value = rough
    bsdf.inputs["Metallic"].default_value = metal
    mat.diffuse_color = col
    return mat


MATS = [material("Sword_Blade", (138, 143, 148), 0.55, 0.6), material("Sword_Rust", (122, 70, 38), 0.9, 0.1),
        material("Sword_Guard", (74, 74, 79), 0.6, 0.5), material("Sword_Grip", (120, 98, 72), 0.95, 0.0)]
BLADE, RUST, GUARD, GRIP = range(4)

bm = bmesh.new()


def ring(z, rx, ry, sides, angle0=0.0):
    return [bm.verts.new((math.cos(angle0 + 2 * math.pi * i / sides) * rx, math.sin(angle0 + 2 * math.pi * i / sides) * ry, z)) for i in range(sides)]


def bridge(a, b, mat):
    n = len(a)
    for i in range(n):
        f = bm.faces.new((a[i], a[(i + 1) % n], b[(i + 1) % n], b[i]))
        f.material_index = mat


def cap(r, mat, flip=False):
    f = bm.faces.new(list(reversed(r)) if flip else r)
    f.material_index = mat


# Grip: an octagonal post with raised cloth bands.
z, prev = -0.08, None
for i in range(9):
    zz = -0.08 + i * 0.02
    r = ring(zz, 0.017 if i % 2 else 0.0155, 0.014 if i % 2 else 0.0125, 8, math.pi / 8)
    if prev is None:
        cap(r, GRIP, flip=True)
    else:
        bridge(prev, r, GRIP)
    prev = r
cap(prev, GRIP)

# Pommel: a squashed octagonal knob.
bottom = ring(-0.115, 0.012, 0.012, 8, math.pi / 8)
mid = ring(-0.1, 0.024, 0.02, 8, math.pi / 8)
top = ring(-0.083, 0.014, 0.012, 8, math.pi / 8)
cap(bottom, GUARD, flip=True)
bridge(bottom, mid, GUARD)
bridge(mid, top, GUARD)
cap(top, GUARD)

# Guard: a plain bar, slightly bent forward at the ends.
for sx in (-1, 1):
    inner = [bm.verts.new((0.0, y, z)) for (y, z) in ((-0.016, 0.082), (0.016, 0.082), (0.016, 0.104), (-0.016, 0.104))]
    outer = [bm.verts.new((sx * 0.095, y * 0.8, z + 0.006)) for (y, z) in ((-0.016, 0.082), (0.016, 0.082), (0.016, 0.104), (-0.016, 0.104))]
    if sx > 0:
        bridge(inner, outer, GUARD)
        cap(outer, GUARD)
    else:
        bridge(outer, inner, GUARD)
        cap(outer, GUARD, flip=True)

# Blade: diamond section (edges at +-x, ridge at +-y), tapering, with chipped edges and rust near the guard.
sections, base, length = 14, 0.104, 0.66
rows = []
for i in range(sections + 1):
    t = i / sections
    zz = base + t * length
    half = 0.024 - 0.008 * t
    if 0 < i < sections and random.random() < 0.35:
        half -= random.uniform(0.003, 0.007)  # a chip in the edge
    thick = 0.0045 - 0.0015 * t
    rows.append([bm.verts.new((half, 0.0, zz)), bm.verts.new((0.0, thick, zz)), bm.verts.new((-half * random.uniform(0.9, 1.0), 0.0, zz)), bm.verts.new((0.0, -thick, zz))])
tip = bm.verts.new((0.0, 0.0, base + length + 0.07))
for i in range(sections):
    rusty_band = i < 2 or (random.random() < 0.18)
    for k in range(4):
        f = bm.faces.new((rows[i][k], rows[i][(k + 1) % 4], rows[i + 1][(k + 1) % 4], rows[i + 1][k]))
        f.material_index = RUST if rusty_band and random.random() < 0.7 else BLADE
for k in range(4):
    f = bm.faces.new((rows[-1][k], rows[-1][(k + 1) % 4], tip))
    f.material_index = BLADE
cap(rows[0], BLADE, flip=True)

mesh = bpy.data.meshes.new("StarterSword")
bm.normal_update()
bm.to_mesh(mesh)
bm.free()
for m in MATS:
    mesh.materials.append(m)
for p in mesh.polygons:
    p.use_smooth = False
obj = bpy.data.objects.new("StarterSword", mesh)
bpy.context.scene.collection.objects.link(obj)
print("[sword] faces", len(mesh.polygons), "length", round(base + length + 0.07 + 0.115, 3))

if OUT:
    os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(filepath=os.path.abspath(OUT), use_selection=True, object_types={"MESH"},
                             apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
                             bake_space_transform=True,  # the axis change goes into the mesh: the Unity root has no rotation
                             bake_anim=False, mesh_smooth_type="FACE", path_mode="AUTO")
    print("[sword] exported", OUT)

if RENDERS:
    os.makedirs(RENDERS, exist_ok=True)
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 300, 600
    scene.world = bpy.data.worlds.new("W")
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = "ORTHO"
    cam.data.ortho_scale = 0.95
    scene.collection.objects.link(cam)
    scene.camera = cam
    for name, yaw in (("front", 0), ("side", 90), ("three", 35)):
        a = math.radians(yaw)
        cam.location = Vector((math.sin(a) * 2.0, -math.cos(a) * 2.0, 0.33))
        cam.rotation_euler = (Vector((0, 0, 0.33)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(RENDERS, "sword_%s.png" % name)
        bpy.ops.render.render(write_still=True)
    print("[sword] renders in", RENDERS)
