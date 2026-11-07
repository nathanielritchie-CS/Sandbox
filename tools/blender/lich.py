# blender --background --factory-startup --python tools/blender/lich.py -- --src art-source/Lich.blend --out <fbx> [--renders <dir>]
# The user's Lich (Meshy "Crystal Wraith", custom rig, one action Float_Glide). Keeps Float_Glide as Idle and adds
# Glide, Cast, Hit and Death, each layered on the floating loop so the robe and wisps never stop. Then exports the FBX.
import argparse
import math
import os
import sys

import bpy
from mathutils import Matrix, Vector

p = argparse.ArgumentParser()
p.add_argument("--src", required=True)
p.add_argument("--out", required=True)
p.add_argument("--renders")
args = p.parse_args(sys.argv[sys.argv.index("--") + 1:])

bpy.ops.wm.open_mainfile(filepath=os.path.abspath(args.src))
scene = bpy.context.scene
rig = bpy.data.objects["CharacterRig"]
mesh = bpy.data.objects["geometry_0"]
idle = bpy.data.actions["Float_Glide"]
idle.name = "Idle"
mesh.data.materials[0].name = "Lich"
LOOP = 47.0  # Float_Glide runs frames 1..48, 48 == 1

X, Y, Z = Vector((1, 0, 0)), Vector((0, 1, 0)), Vector((0, 0, 1))  # the rig faces -Y; +X turns would tip the top forward
REST = {b.name: b.matrix_local.to_3x3() for b in rig.data.bones}
ROBE = [b.name for b in rig.data.bones if b.name.startswith("robe_")]
SLEEVES = [b.name for b in rig.data.bones if b.name.startswith("sleeve_")]
WISP = ["wisp_1", "wisp_2", "wisp_3"]


def turn(bone, axis, deg):
    # A turn about an armature axis, as a rotation in the bone's own rest frame.
    b = REST[bone]
    return (b.inverted() @ Matrix.Rotation(math.radians(deg), 3, axis) @ b).to_quaternion()


def ease(t, keys):
    # Smooth curve through (frame, value) keys, held flat before the first and after the last.
    if t <= keys[0][0]:
        return keys[0][1]
    for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
        if t <= f1:
            u = (t - f0) / (f1 - f0)
            return v0 + (v1 - v0) * u * u * (3 - 2 * u)
    return keys[-1][1]


def sample_idle(g):
    rig.animation_data.action = idle
    scene.frame_set(int(g), subframe=g - int(g))
    return {pb.name: (pb.location.copy(), pb.rotation_euler.to_quaternion()) for pb in rig.pose.bones}


def make(name, frames, idle_frame, turns, sink=None):
    """turns: [(bones, axis, keys)], keys in clip frames; sink: (axis, keys) moves the root bone, in rig units."""
    poses = [sample_idle(1 + idle_frame(f) % LOOP) for f in range(1, frames + 1)]
    act = bpy.data.actions.new(name)
    rig.animation_data.action = act
    for f, base in zip(range(1, frames + 1), poses):
        for pb in rig.pose.bones:
            loc, rot = base[pb.name]
            for bones, axis, keys in turns:
                if pb.name in bones:
                    rot = turn(pb.name, axis, ease(f, keys)) @ rot
            if sink and pb.name == "root":
                axis, keys = sink
                loc = loc + REST["root"].inverted() @ (axis * ease(f, keys))
            pb.location = loc
            pb.rotation_euler = rot.to_euler("XYZ", pb.rotation_euler)
            pb.keyframe_insert("location", frame=f)
            pb.keyframe_insert("rotation_euler", frame=f)
    return act


tips = [b for b in ROBE if not b.endswith("_1")]
make("Glide", 48, lambda f: (f - 1) * 47 / 48 * 1.25, [
    (["body"], X, [(1, 14)]),
    (["chest"], X, [(1, 4)]),
    (["head"], X, [(1, -10)]),
    (tips, X, [(1, 10)]),
    (WISP, X, [(1, 14)]),
    (SLEEVES, X, [(1, 10)]),
    (["upperarm.R"], Z, [(1, 12)]),
    (["upperarm.L"], Z, [(1, -12)]),
])

# Cast: wind up (staff raised and back) to frame 12, thrust at 16 (a bolt would leave here, 0.63 s), recover by 40.
CAST = [1, 12, 16, 22, 40]
make("Cast", 40, lambda f: f - 1, [
    (["body"], X, list(zip(CAST, [0, -10, 14, 12, 0]))),
    (["chest"], X, list(zip(CAST, [0, -8, 8, 6, 0]))),
    (["head"], X, list(zip(CAST, [0, -6, 4, 4, 0]))),
    (["upperarm.R"], Y, list(zip(CAST, [0, -55, -20, -20, 0]))),
    (["upperarm.R"], Z, list(zip(CAST, [0, 10, -50, -45, 0]))),
    (["forearm.R"], Y, list(zip(CAST, [0, -25, 0, 0, 0]))),
    (["staff"], X, list(zip(CAST, [0, -20, 45, 40, 0]))),
    (["upperarm.L"], Y, list(zip(CAST, [0, 30, 10, 10, 0]))),
    (["upperarm.L"], Z, list(zip(CAST, [0, 0, 55, 50, 0]))),
    (["forearm.L"], Z, list(zip(CAST, [0, 0, 15, 15, 0]))),
    (tips, X, list(zip(CAST, [0, -6, 12, 10, 0]))),
    (WISP, X, list(zip(CAST, [0, -8, 18, 14, 0]))),
])

HIT = [1, 4, 18]
make("Hit", 18, lambda f: f - 1, [
    (["body"], X, list(zip(HIT, [0, -16, 0]))),
    (["chest"], X, list(zip(HIT, [0, -10, 0]))),
    (["head"], X, list(zip(HIT, [0, -14, 0]))),
    (["upperarm.R"], Y, list(zip(HIT, [0, -25, 0]))),
    (["upperarm.L"], Y, list(zip(HIT, [0, 25, 0]))),
    (tips, X, list(zip(HIT, [0, -10, 0]))),
    (WISP, X, list(zip(HIT, [0, -15, 0]))),
], sink=(Y, list(zip(HIT, [0, 0.04, 0]))))

# Death: recoil (5), slump (18), then sink out of sight by 48. The float slows to a stop.
DEATH = [1, 5, 18, 30, 48]
make("Death", 48, lambda f: min(f - 1, 20) * 0.5, [
    (["body"], X, list(zip(DEATH, [0, -18, 10, 18, 20]))),
    (["chest"], X, list(zip(DEATH, [0, -10, 6, 10, 10]))),
    (["head"], X, list(zip(DEATH, [0, -15, 12, 22, 25]))),
    (["upperarm.R"], Y, list(zip(DEATH, [0, -25, 40, 55, 60]))),
    (["upperarm.L"], Y, list(zip(DEATH, [0, 25, -40, -55, -60]))),
    (["staff"], X, list(zip(DEATH, [0, 0, 20, 60, 80]))),
    (tips, X, list(zip(DEATH, [0, -10, 10, 20, 20]))),
    (WISP, X, list(zip(DEATH, [0, -15, 10, 25, 25]))),
], sink=(Z, list(zip(DEATH, [0, 0, -0.05, -0.35, -0.9]))))  # sinks right into the ground

rig.animation_data.action = idle

if args.renders:
    os.makedirs(args.renders, exist_ok=True)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "TEXTURE"
    scene.render.resolution_x, scene.render.resolution_y = 400, 500
    cam = bpy.data.objects.new("ReviewCam", bpy.data.cameras.new("ReviewCam"))
    cam.data.type, cam.data.ortho_scale = "ORTHO", 1.25
    scene.collection.objects.link(cam)
    scene.camera = cam
    yaw = math.radians(-50)  # front three-quarter from the staff side, so leans and the thrust read
    cam.location = Vector((-math.sin(yaw) * 4, -math.cos(yaw) * 4, 0.45))
    cam.rotation_euler = (Vector((0, 0, 0.45)) - cam.location).to_track_quat("-Z", "Y").to_euler()
    for act in bpy.data.actions:
        rig.animation_data.action = act
        first, last = (int(v) for v in act.frame_range)
        for i in range(6):
            f = round(first + (last - first) * i / 5)
            scene.frame_set(f)
            scene.render.filepath = os.path.join(args.renders, "%s_%03d.png" % (act.name, f))
            bpy.ops.render.render(write_still=True)
    rig.animation_data.action = idle

bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
mesh.select_set(True)
bpy.context.view_layer.objects.active = rig
os.makedirs(os.path.dirname(os.path.abspath(args.out)), exist_ok=True)
bpy.ops.export_scene.fbx(
    filepath=os.path.abspath(args.out), use_selection=True, object_types={"ARMATURE", "MESH"},
    apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL", axis_forward="-Z", axis_up="Y",
    add_leaf_bones=False, primary_bone_axis="Y", secondary_bone_axis="X", armature_nodetype="NULL",
    bake_anim=True, bake_anim_use_all_bones=True, bake_anim_use_nla_strips=False, bake_anim_use_all_actions=True,
    bake_anim_force_startend_keying=True, bake_anim_step=1.0, bake_anim_simplify_factor=0.0,
    mesh_smooth_type="FACE", use_armature_deform_only=False, path_mode="STRIP")
print("LICH_DONE", args.out)
