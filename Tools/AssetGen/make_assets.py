"""Builds the Magic Sort glass tube, its cap and the liquid segment in 3D and renders them as 2D sprites.

Usage: blender -b --factory-startup -P make_assets.py -- <out_dir> [tube|cap|water|bg|all] [samples]
"""
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else os.path.join(os.path.dirname(__file__), "out")
WHAT = argv[1] if len(argv) > 1 else "all"
SAMPLES = int(argv[2]) if len(argv) > 2 else 128
os.makedirs(OUT, exist_ok=True)

TILT_DEG = 14.0  # matches the top-ellipse angle of the existing WaterSmall sprite


# ---------------------------------------------------------------- scene setup
def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.samples = SAMPLES
    scene.cycles.use_denoising = True
    scene.cycles.max_bounces = 16
    scene.cycles.transmission_bounces = 16
    scene.cycles.glossy_bounces = 8
    scene.cycles.transparent_max_bounces = 16
    scene.render.film_transparent = True
    scene.cycles.film_transparent_glass = True
    scene.cycles.film_transparent_roughness = 0.1
    scene.view_settings.view_transform = "Standard"
    scene.view_settings.look = "None"
    scene.render.image_settings.file_format = "PNG"
    scene.render.image_settings.color_mode = "RGBA"
    scene.render.image_settings.color_depth = "8"
    scene.render.resolution_percentage = 100
    try_gpu(scene)
    return scene


def try_gpu(scene):
    try:
        prefs = bpy.context.preferences.addons["cycles"].preferences
    except KeyError:
        return
    for backend in ("OPTIX", "CUDA", "HIP", "ONEAPI", "METAL"):
        try:
            prefs.compute_device_type = backend
            prefs.get_devices()
        except Exception:
            continue
        gpus = [d for d in prefs.devices if d.type != "CPU"]
        if gpus:
            for d in prefs.devices:
                d.use = d.type != "CPU"
            scene.cycles.device = "GPU"
            print(f"[assets] using GPU backend {backend}: {[d.name for d in gpus]}")
            return
    print("[assets] using CPU")


def set_world(color, strength):
    world = bpy.data.worlds.new("World")
    bpy.context.scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (*color, 1.0)
    bg.inputs["Strength"].default_value = strength


def emissive_card(name, size, location, look_at, strength, color=(1, 1, 1)):
    """Emissive plane: shows up as a crisp highlight strip in glass reflections, invisible to camera."""
    bpy.ops.mesh.primitive_plane_add(size=1.0, location=location)
    card = bpy.context.active_object
    card.name = name
    card.scale = (size[0], size[1], 1.0)
    direction = Vector(look_at) - Vector(location)
    card.rotation_euler = direction.to_track_quat("Z", "Y").to_euler()
    mat = bpy.data.materials.new(name + "Mat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Color"].default_value = (*color, 1.0)
    emit.inputs["Strength"].default_value = strength
    out = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(emit.outputs[0], out.inputs[0])
    card.data.materials.append(mat)
    card.visible_camera = False
    card.visible_shadow = False
    card.visible_transmission = False
    return card


def make_camera():
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam = bpy.data.objects.new("Cam", cam_data)
    bpy.context.scene.collection.objects.link(cam)
    bpy.context.scene.camera = cam
    t = math.radians(TILT_DEG)
    cam.rotation_euler = (math.radians(90.0) - t, 0.0, 0.0)
    return cam


def fit_camera(cam, obj, res, top_px, bottom_px, target_z, max_width_px=None):
    """Ortho camera tilted down by TILT_DEG; scaled so obj spans rows [top_px, bottom_px] (or max_width_px)."""
    scene = bpy.context.scene
    scene.render.resolution_x, scene.render.resolution_y = res
    t = math.radians(TILT_DEG)
    dist = 20.0
    cam.location = (0.0, -dist * math.cos(t), target_z + dist * math.sin(t))
    bpy.context.view_layer.update()

    inv = cam.matrix_world.inverted()
    deps = bpy.context.evaluated_depsgraph_get()
    eval_obj = obj.evaluated_get(deps)
    pts = [inv @ (eval_obj.matrix_world @ v.co) for v in eval_obj.data.vertices]
    xs = [p.x for p in pts]
    ys = [p.y for p in pts]
    span_y = max(ys) - min(ys)
    px_per_unit = (bottom_px - top_px) / span_y
    if max_width_px:
        px_per_unit = min(px_per_unit, max_width_px / (max(xs) - min(xs)))
    cam.data.ortho_scale = max(res) / px_per_unit

    # move camera along its up axis so the bbox centre lands on the requested rows
    want_center_row = (top_px + bottom_px) / 2.0
    offset_px = want_center_row - res[1] / 2.0
    bbox_center_y = (max(ys) + min(ys)) / 2.0
    up = cam.matrix_world.to_3x3() @ Vector((0, 1, 0))
    cam.location += up * (bbox_center_y + offset_px / px_per_unit)
    bpy.context.view_layer.update()
    width_px = (max(xs) - min(xs)) * px_per_unit
    print(f"[assets] {obj.name}: px/unit={px_per_unit:.1f} width_px={width_px:.1f} ortho={cam.data.ortho_scale:.3f}")
    return px_per_unit


# ---------------------------------------------------------------- geometry
def smoothstep(t):
    return t * t * (3.0 - 2.0 * t)


def arc(cx, cz, r, a0, a1, n):
    return [(cx + r * math.cos(math.radians(a0 + (a1 - a0) * i / n)),
             cz + r * math.sin(math.radians(a0 + (a1 - a0) * i / n))) for i in range(n + 1)]


def blend(p0, p1, n):
    """S-curve between two points with vertical tangents at both ends."""
    return [(p0[0] + (p1[0] - p0[0]) * smoothstep(i / n), p0[1] + (p1[1] - p0[1]) * i / n) for i in range(n + 1)]


def line(p0, p1, n):
    return [(p0[0] + (p1[0] - p0[0]) * i / n, p0[1] + (p1[1] - p0[1]) * i / n) for i in range(n + 1)]


def tube_profile():
    """Outer (r, z) profile of the vial; proportions follow the original Tube.png silhouette."""
    pts = []
    pts += line((0.0, 0.0), (0.70, 0.0), 6)
    pts += arc(0.70, 0.30, 0.30, -90, 0, 14)[1:]          # rounded bottom edge
    pts += line((1.0, 0.30), (1.0, 4.00), 24)[1:]          # straight body (9-slice centre lives here)
    pts += blend((1.0, 4.00), (0.64, 4.38), 16)[1:]        # shoulder
    pts += line((0.64, 4.38), (0.64, 4.50), 3)[1:]         # neck
    pts += blend((0.64, 4.50), (0.80, 4.60), 8)[1:]        # lip flare
    pts += line((0.80, 4.60), (0.80, 4.88), 6)[1:]         # lip side
    pts += arc(0.70, 4.88, 0.10, 0, 90, 8)[1:]             # lip top round
    pts += line((0.70, 4.98), (0.63, 4.98), 2)[1:]
    pts += arc(0.63, 4.91, 0.07, 90, 180, 6)[1:]           # lip inner round
    pts += line((0.56, 4.91), (0.56, 4.55), 4)[1:]         # down into the neck
    return pts


def cap_profile():
    """Solid (r, z) profile of a cap that sits over the tube lip (r 0.80, z 4.50 to 4.98) with a little play."""
    pts = []
    pts += line((0.0, 5.035), (0.60, 5.035), 6)                 # slightly sunken top face
    pts += arc(0.60, 5.06, 0.025, -90, 0, 4)[1:]
    pts += line((0.625, 5.06), (0.78, 5.06), 3)[1:]
    pts += arc(0.78, 4.98, 0.08, 90, 0, 8)[1:]                   # rounded top edge
    pts += line((0.86, 4.98), (0.86, 4.58), 16)[1:]              # gripped side, ridges added later
    pts += arc(0.83, 4.58, 0.03, 0, -90, 4)[1:]                  # rounded bottom edge
    pts += line((0.83, 4.55), (0.0, 4.55), 6)[1:]
    return pts


CAP_RIDGES = 40


def add_ridges(obj, z0, z1, count, depth):
    """Fluted grip on the cap side: pushes the side vertices in and out with the angle around the axis."""
    for v in obj.data.vertices:
        x, y, z = v.co
        r = math.hypot(x, y)
        if r < 0.5 or not (z0 <= z <= z1):
            continue
        # fade the flutes out toward the rounded edges
        fade = min(1.0, (z - z0) / 0.04, (z1 - z) / 0.04)
        a = math.atan2(y, x)
        k = 1.0 - depth * fade * (0.5 - 0.5 * math.cos(count * a)) / r
        v.co = (x * k, y * k, z)
    obj.data.update()


def plastic_material():
    mat = bpy.data.materials.new("CapPlastic")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (0.85, 0.85, 0.85, 1.0)  # tinted with the solved color in Unity
    bsdf.inputs["Roughness"].default_value = 0.35
    bsdf.inputs["Coat Weight"].default_value = 0.6
    bsdf.inputs["Coat Roughness"].default_value = 0.06
    return mat


def revolve(name, profile, thickness, steps=128):
    mesh = bpy.data.meshes.new(name)
    verts = [(r, 0.0, z) for r, z in profile]
    edges = [(i, i + 1) for i in range(len(verts) - 1)]
    mesh.from_pydata(verts, edges, [])
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)

    screw = obj.modifiers.new("Screw", "SCREW")
    screw.axis = "Z"
    screw.angle = math.radians(360)
    screw.steps = steps
    screw.render_steps = steps
    screw.use_merge_vertices = True
    screw.merge_threshold = 1e-4
    screw.use_normal_calculate = True
    if thickness > 0:
        solid = obj.modifiers.new("Solidify", "SOLIDIFY")
        solid.thickness = thickness
        solid.offset = 0.0
        solid.use_even_offset = True

    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    for m in list(obj.modifiers):
        bpy.ops.object.modifier_apply(modifier=m.name)

    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-4)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    bm.to_mesh(obj.data)
    bm.free()
    for p in obj.data.polygons:
        p.use_smooth = True
    return obj


def glass_material():
    mat = bpy.data.materials.new("Glass")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)  # tint is applied by the TubeGlass shader
    bsdf.inputs["Roughness"].default_value = 0.02
    bsdf.inputs["IOR"].default_value = 1.45
    bsdf.inputs["Transmission Weight"].default_value = 1.0
    return mat


def liquid_material():
    mat = bpy.data.materials.new("LiquidWhite")
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (1.0, 1.0, 1.0, 1.0)
    bsdf.inputs["Roughness"].default_value = 0.28
    bsdf.inputs["Specular IOR Level"].default_value = 0.35
    bsdf.inputs["Coat Weight"].default_value = 0.4
    bsdf.inputs["Coat Roughness"].default_value = 0.08
    return mat


# ---------------------------------------------------------------- renders
def render(path):
    bpy.context.scene.render.filepath = path
    bpy.ops.render.render(write_still=True)
    print(f"[assets] wrote {path}")


def backdrop(cam, strength):
    """Emissive wall behind the subject, seen by the camera and through the glass but by nothing else."""
    view = cam.matrix_world.to_3x3() @ Vector((0, 0, -1))
    bpy.ops.mesh.primitive_plane_add(size=60.0, location=Vector((0, 0, 2.5)) + view * 12.0)
    wall = bpy.context.active_object
    wall.name = "Backdrop"
    wall.rotation_euler = (-view).to_track_quat("Z", "Y").to_euler()
    mat = bpy.data.materials.new("BackdropMat")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    emit = nodes.new("ShaderNodeEmission")
    emit.inputs["Strength"].default_value = strength
    out = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(emit.outputs[0], out.inputs[0])
    wall.data.materials.append(mat)
    wall.visible_diffuse = False
    wall.visible_glossy = False
    wall.visible_shadow = False
    return wall


def render_linear(path):
    """Renders to a float EXR (scene linear, no view transform) and returns its pixels, top row first."""
    settings = bpy.context.scene.render.image_settings
    settings.file_format = "OPEN_EXR"
    settings.color_depth = "32"
    render(path)
    img = bpy.data.images.load(path)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    settings.file_format = "PNG"
    settings.color_depth = "8"
    return px


def linear_to_srgb(c):
    c = np.clip(c, 0.0, 1.0)
    return np.where(c <= 0.0031308, c * 12.92, 1.055 * np.power(c, 1.0 / 2.4) - 0.055)


def save_rgba(path, rgba):
    """Writes a byte PNG from top-row-first pixels; rgb must already be sRGB encoded."""
    h, w = rgba.shape[:2]
    img = bpy.data.images.new(os.path.basename(path), w, h, alpha=True)
    img.alpha_mode = "STRAIGHT"
    img.pixels.foreach_set(np.ascontiguousarray(rgba[::-1]).astype(np.float32).ravel())
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    print(f"[assets] wrote {path}")


def tube_scene():
    """The glass tube under the highlight cards, with the camera fitted to the Tube.png canvas."""
    reset_scene()
    # a dim, cool surround: the glass edges pick it up as a tinted line instead of a white outline
    set_world((0.42, 0.55, 0.85), 0.45)
    tube = revolve("Tube", tube_profile(), thickness=0.035)
    tube.data.materials.append(glass_material())

    # softbox strips -> crisp highlight bands: a strong one on the left, a thin one on the right
    emissive_card("KeyBand", (2.2, 9.0), (-4.5, -6.0, 3.0), (0, 0, 2.6), 2.5)
    emissive_card("KeyStrip", (0.35, 9.0), (-4.2, -6.4, 3.0), (0, 0, 2.6), 9.0)
    emissive_card("FillStrip", (0.25, 9.0), (5.0, -5.0, 3.0), (0, 0, 2.6), 3.0)
    emissive_card("TopCard", (6.0, 6.0), (0.0, -2.0, 11.0), (0, 0, 4.8), 1.5)

    cam = make_camera()
    # Tube.png is 696x1676 with 9-slice borders bottom=753, top=892 -> keep the same canvas
    fit_camera(cam, tube, (696, 1676), 50, 1652, target_z=2.5, max_width_px=606)
    return tube, cam


def render_tube():
    """Glass as a premultiplied sprite, pulled from two renders over a black and a white backdrop:
    alpha is how much of the backdrop the glass hides, rgb is the light the glass adds on its own
    (reflections and refracted surround). The highlights add light even where the glass is clear,
    which a straight-alpha sprite cannot hold, so TubeGlass blends it premultiplied."""
    tube_scene()
    scene = bpy.context.scene
    scene.render.film_transparent = False
    cam = scene.camera
    wall = backdrop(cam, 0.0)
    over_black = render_linear(os.path.join(OUT, "tube_black.exr"))
    wall.data.materials[0].node_tree.nodes["Emission"].inputs["Strength"].default_value = 1.0
    over_white = render_linear(os.path.join(OUT, "tube_white.exr"))

    alpha = np.clip(1.0 - (over_white[..., :3] - over_black[..., :3]).mean(axis=2), 0.0, 1.0)
    rgb = over_black[..., :3]
    # the matte is noise outside the glass; keep only what the glass actually covers
    alpha[alpha < 0.01] = 0.0
    rgb = rgb * (alpha[..., None] > 0.0)
    save_rgba(os.path.join(OUT, "Tube.png"), np.concatenate([linear_to_srgb(rgb), alpha[..., None]], axis=2))


def render_cap():
    """The cap seen through the tube camera, cropped to the rows it covers so its top edge is the top of
    Tube.png: placed with a top-center pivot at the top of the tube sprite, it sits right on the lip."""
    tube, cam = tube_scene()
    tube.hide_render = True
    set_world((0.92, 0.95, 1.0), 0.22)
    key = bpy.data.lights.new("Key", "AREA")
    key.energy = 320
    key.size = 5.0
    key_obj = bpy.data.objects.new("Key", key)
    key_obj.location = (-4.0, -5.0, 10.0)
    key_obj.rotation_euler = (Vector((0, 0, 4.8)) - key_obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.collection.objects.link(key_obj)

    cap = revolve("Cap", cap_profile(), thickness=0.0, steps=8 * CAP_RIDGES)
    add_ridges(cap, 4.58, 4.98, CAP_RIDGES, 0.018)
    cap.data.materials.append(plastic_material())

    full = os.path.join(OUT, "cap_full.png")
    render(full)
    img = bpy.data.images.load(full)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    rows = np.nonzero(px[..., 3].max(axis=1) > 0.0)[0]
    bottom = int(rows.max()) + 4
    print(f"[assets] cap covers rows {int(rows.min())}..{int(rows.max())} of {h}")
    # pixels come back as stored, already sRGB
    save_rgba(os.path.join(OUT, "Cap.png"), px[:bottom])


def render_water():
    reset_scene()
    set_world((1.0, 1.0, 1.0), 0.55)
    r, h, fillet = 1.0, 1.32, 0.10
    prof = []
    prof += line((0.0, 0.0), (r - fillet, 0.0), 4)
    prof += arc(r - fillet, fillet, fillet, -90, 0, 6)[1:]
    prof += line((r, fillet), (r, h - fillet), 8)[1:]
    prof += arc(r - fillet, h - fillet, fillet, 0, 90, 6)[1:]
    prof += line((r - fillet, h), (0.0, h), 4)[1:]
    seg = revolve("WaterSeg", prof, thickness=0.0)
    seg.data.materials.append(liquid_material())

    key = bpy.data.lights.new("Key", "AREA")
    key.energy = 220
    key.size = 4.0
    key_obj = bpy.data.objects.new("Key", key)
    key_obj.location = (-2.5, -3.0, 8.0)
    key_obj.rotation_euler = (Vector((0, 0, 0.6)) - key_obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.collection.objects.link(key_obj)
    emissive_card("Strip", (0.5, 4.0), (-3.5, -4.0, 0.7), (0, 0, 0.7), 2.0)

    cam = make_camera()
    fit_camera(cam, seg, (256, 256), 14, 242, target_z=h / 2, max_width_px=248)
    render(os.path.join(OUT, "WaterSmall.png"))


def render_bg():
    """Flat fallback for Bg.png (the BgCasual shader replaces it at runtime)."""
    w, h = 1448, 1086
    y, x = np.mgrid[0:h, 0:w].astype(np.float32)
    v = 1.0 - y / (h - 1)   # numpy row 0 = top of the picture
    u = x / (w - 1)
    top = np.array([0.37, 0.66, 1.00])
    bottom = np.array([0.63, 0.47, 0.98])
    col = bottom[None, None, :] * (1 - v[..., None]) + top[None, None, :] * v[..., None]
    glow = np.exp(-(((u - 0.5) * 1.6) ** 2 + ((v - 0.55) * 1.4) ** 2) * 2.5)
    col = col + glow[..., None] * 0.18
    rng = np.random.default_rng(7)
    for _ in range(18):
        cx, cy, rad = rng.uniform(0, w), rng.uniform(0, h), rng.uniform(25, 110)
        d = np.sqrt((x - cx) ** 2 + (y - cy) ** 2)
        a = np.clip((rad - d) / 3.0, 0, 1) * rng.uniform(0.06, 0.14)
        col = col * (1 - a[..., None]) + a[..., None]
    col = np.clip(col, 0, 1)   # byte image: pixels are stored as sRGB values
    rgba = np.concatenate([col, np.ones((h, w, 1))], axis=2).astype(np.float32)
    img = bpy.data.images.new("Bg", w, h, alpha=True)
    img.pixels.foreach_set(rgba[::-1].ravel())   # numpy row 0 = top, Blender row 0 = bottom
    img.filepath_raw = os.path.join(OUT, "Bg.png")
    img.file_format = "PNG"
    img.save()
    print(f"[assets] wrote {img.filepath_raw}")


if WHAT in ("tube", "all"):
    render_tube()
if WHAT in ("cap", "all"):
    render_cap()
if WHAT in ("water", "all"):
    render_water()
if WHAT in ("bg", "all"):
    render_bg()
