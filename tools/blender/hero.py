# The user's Meshy hero (spec 20): flat-colour regions + every animation as an FBX take.
#   blender --background --factory-startup --python tools/blender/hero.py -- --glb <file.glb> [--out <file.fbx>] [--renders <dir>]
# Regions come from skin weights and height in the bind pose (the GLB has no texture). Re-run after re-exporting from Meshy.
import math
import os
import sys

import bmesh
import bpy
from mathutils import Vector

ARGV = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []


def arg(name, default=None):
    return ARGV[ARGV.index(name) + 1] if name in ARGV else default


GLB, OUT, RENDERS = arg("--glb"), arg("--out"), arg("--renders")

# Region -> default colour (sRGB), the appearance catalog's defaults.
REGIONS = {"Skin": (240, 200, 170), "Hair": (70, 46, 36), "Top": (58, 112, 186), "Trousers": (56, 60, 78), "Boots": (112, 74, 48)}

# Tuning, as fractions between landmarks (see classify()).
BOOT_TOP = 0.45     # ankle -> knee
BELT = 0.55         # hips joint -> spine1 joint
HAIR_FRONT = 0.66   # jaw (head joint) -> head top, in front of the ears
HAIR_SIDE = 0.38    # same, beside the head
HAIR_BACK = 0.05    # same, behind the ears
FRONT_DEPTH = 0.025 # m in front of the head joint counts as "front"
BACK_DEPTH = 0.035  # m behind the head joint counts as "back"

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.abspath(GLB))
scene = bpy.context.scene
rig = next(o for o in scene.objects if o.type == "ARMATURE")
body = next(o for o in scene.objects if o.type == "MESH")


def short(name):
    return name.split(":")[-1]


def joint(name):
    bone = next(b for b in rig.data.bones if short(b.name) == name)
    return rig.matrix_world @ bone.head_local


J = {n: joint(n) for n in ("Hips", "Spine1", "Head", "HeadTop_End", "LeftLeg", "LeftFoot")}
print("[hero] joints", {k: tuple(round(c, 3) for c in v) for k, v in J.items()})
boot_top = J["LeftFoot"].z + BOOT_TOP * (J["LeftLeg"].z - J["LeftFoot"].z)
belt = J["Hips"].z + BELT * (J["Spine1"].z - J["Hips"].z)
jaw, top = J["Head"].z, J["HeadTop_End"].z
head_y = J["Head"].y  # Blender: -Y is the character's front

FEET = {"LeftFoot", "RightFoot", "LeftToeBase", "RightToeBase", "LeftToe_End", "RightToe_End"}
SHINS = {"LeftLeg", "RightLeg"}
THIGHS = {"LeftUpLeg", "RightUpLeg"}
TORSO = {"Hips", "Spine", "Spine1", "Spine2", "LeftShoulder", "RightShoulder"}
UPPER_ARMS = {"LeftArm", "RightArm"}  # elbow-length sleeves: the border follows the elbow's weights
HEAD = {"Head", "HeadTop_End", "headfront"}


def hair(point):
    t = (point.z - jaw) / (top - jaw)
    depth = point.y - head_y
    limit = HAIR_FRONT if depth < -FRONT_DEPTH else HAIR_BACK if depth > BACK_DEPTH else HAIR_SIDE
    return t > limit


def classify(point, bone):
    if bone in FEET or (bone in SHINS and point.z < boot_top):
        return "Boots"
    if bone in SHINS or bone in THIGHS or (bone in TORSO and point.z < belt):
        return "Trousers"
    if bone in TORSO or bone in UPPER_ARMS:
        return "Top"
    if bone in HEAD and hair(point):
        return "Hair"
    return "Skin"


def srgb_to_linear(c):
    c /= 255.0
    return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4


mesh = body.data
# Straight waist and boot-top borders: cut every face crossing those heights (weights and UVs interpolate).
bm = bmesh.new()
bm.from_mesh(mesh)
to_local = body.matrix_world.inverted()
for height in (belt, boot_top):
    bmesh.ops.bisect_plane(bm, geom=bm.verts[:] + bm.edges[:] + bm.faces[:],
                           plane_co=to_local @ Vector((0, 0, height)), plane_no=(to_local.to_3x3() @ Vector((0, 0, 1))).normalized())
bm.to_mesh(mesh)
bm.free()
mesh.materials.clear()
slot = {}
for name, rgb in REGIONS.items():
    mat = bpy.data.materials.new("Hero_" + name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    linear = tuple(srgb_to_linear(c) for c in rgb) + (1.0,)
    bsdf.inputs["Base Color"].default_value = linear
    bsdf.inputs["Roughness"].default_value = 0.8
    mat.diffuse_color = linear  # Workbench preview
    mesh.materials.append(mat)
    slot[name] = len(mesh.materials) - 1

groups = {g.index: short(g.name) for g in body.vertex_groups}
to_world = body.matrix_world
counts = dict.fromkeys(REGIONS, 0)
for poly in mesh.polygons:
    weights = {}
    for vi in poly.vertices:
        for g in mesh.vertices[vi].groups:
            weights[groups[g.group]] = weights.get(groups[g.group], 0.0) + g.weight
    bone = max(weights, key=weights.get) if weights else "Hips"
    region = classify(to_world @ poly.center, bone)
    poly.material_index = slot[region]
    counts[region] += 1
print("[hero] faces per region", counts, "boot top", round(boot_top, 3), "belt", round(belt, 3), "jaw", round(jaw, 3), "top", round(top, 3))
print("[hero] actions", [(a.name, round(a.frame_range[1] / scene.render.fps, 2)) for a in bpy.data.actions], "fps", scene.render.fps)

if OUT:
    os.makedirs(os.path.dirname(os.path.abspath(OUT)), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.export_scene.fbx(
        filepath=os.path.abspath(OUT), use_selection=True, object_types={"ARMATURE", "MESH"},
        apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
        add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
        bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
        bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
        mesh_smooth_type="FACE", use_armature_deform_only=False, path_mode="AUTO")
    print("[hero] exported", OUT)

if RENDERS:
    os.makedirs(RENDERS, exist_ok=True)
    ad = rig.animation_data
    if ad:
        for track in ad.nla_tracks:
            track.mute = True
        ad.action = None  # bind pose
    scene.render.engine = "BLENDER_WORKBENCH"
    shading = scene.display.shading
    shading.light = "STUDIO"
    shading.color_type = "MATERIAL"
    shading.show_cavity = True
    scene.render.resolution_x, scene.render.resolution_y = 360, 480
    scene.world = bpy.data.worlds.new("W")
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.type = "ORTHO"
    scene.collection.objects.link(cam)
    scene.camera = cam
    for name, yaw, height, scale in (("front", 0, 0.9, 2.0), ("side", 90, 0.9, 2.0), ("back", 180, 0.9, 2.0),
                                     ("head_front", 0, top - 0.12, 0.42), ("head_side", 90, top - 0.12, 0.42), ("head_back", 180, top - 0.12, 0.42)):
        a = math.radians(yaw)
        cam.data.ortho_scale = scale
        cam.location = Vector((math.sin(a) * 4.0, -math.cos(a) * 4.0, height))
        cam.rotation_euler = (Vector((0, 0, height)) - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(RENDERS, "rest_%s.png" % name)
        bpy.ops.render.render(write_still=True)
    print("[hero] renders in", RENDERS)
