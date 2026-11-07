# blender --background --factory-startup --python tools/blender/contact_sheet.py -- <renders dir> <out dir>
# Contact sheets: one row per action (its frames left to right), 4 actions per sheet, half resolution.
import os
import sys

import bpy
import numpy as np

src, out = sys.argv[sys.argv.index("--") + 1:][:2]
os.makedirs(out, exist_ok=True)
files = sorted(f for f in os.listdir(src) if f.endswith(".png") and not f.startswith(("rest_", "face")))
actions = {}
for f in files:
    name, frame = f[:-4].rsplit("_", 1)
    actions.setdefault(name, []).append((int(frame), f))
order = ["Idle", "Walk", "Run", "WalkBack", "RunBack", "WalkLeft", "RunLeft", "WalkRight", "RunRight",
         "Attack1", "Attack2", "Attack3", "Charge", "Heavy", "Hit", "Roll", "Death"]
order = [a for a in order if a in actions] + [a for a in actions if a not in order]


def load(path):
    img = bpy.data.images.load(path)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)
    bpy.data.images.remove(img)
    return px[::2, ::2]


for sheet_i in range(0, len(order), 4):
    rows = []
    for name in order[sheet_i:sheet_i + 4]:
        frames = [load(os.path.join(src, f)) for _, f in sorted(actions[name])]
        while len(frames) < 6:
            frames.append(np.zeros_like(frames[0]))
        rows.append(np.concatenate(frames[:6], axis=1))
    sheet = np.concatenate(rows[::-1], axis=0)  # images are stored bottom-up
    h, w = sheet.shape[:2]
    img = bpy.data.images.new("sheet", w, h, alpha=True)
    img.pixels[:] = sheet.ravel()
    path = os.path.join(out, "sheet_%d_%s.png" % (sheet_i // 4, "_".join(order[sheet_i:sheet_i + 4])))
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    print("sheet", path)
