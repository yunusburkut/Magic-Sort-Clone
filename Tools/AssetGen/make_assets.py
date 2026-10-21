"""Builds the Magic Sort glass bottle, its cork and the liquid segment in 3D and renders them as 2D sprites.

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
    return px_per_unit, span_y * px_per_unit


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


# Magic Sort's milk bottle: straight body on a rounded bottom, a long soft shoulder into a short wide neck and a thick
# rolled glass lip, closed by a tapered cork when solved. Outer radius of the body is 1.
GLASS_T = 0.03          # wall thickness: thin walls read as thin edges in the sprite
BOTTOM_H = 0.60         # how far up the walls the rounded bottom reaches
BOTTOM_P = 2.3          # superellipse power of the bottom: between a bowl and a rounded box
SHOULDER_Z = 2.64       # top of the straight body; the sprite's 9-slice centre sits below this
NECK_R = 0.43
NECK_Z = SHOULDER_Z + 0.62
LIP_Z = NECK_Z + 0.06   # bottom of the rolled lip
LIP_R = 0.58            # outer radius of the lip
LIP_H = 0.22
TOP_Z = LIP_Z + LIP_H
MOUTH_R = NECK_R - GLASS_T

# the cork: narrow end pushed into the mouth down to about the bottom of the lip, wider top standing above it
CORK_R0 = MOUTH_R - 0.01
CORK_R1 = 0.46
CORK_BOTTOM = TOP_Z - 0.20
CORK_TOP = TOP_Z + 0.36


def offset_inward(pts, t):
    """Moves each (r, z) point of an outer profile running up and outward by t toward the inside of the glass."""
    out = []
    for i, (r, z) in enumerate(pts):
        a = pts[max(i - 1, 0)]
        b = pts[min(i + 1, len(pts) - 1)]
        dr, dz = b[0] - a[0], b[1] - a[1]
        length = math.hypot(dr, dz)
        out.append((max(0.0, r - t * dz / length), z + t * dr / length))
    return out


def tube_profile():
    """Closed (r, z) cross-section of the glass, axis to axis: up the outside, over the mouth, down the inside."""
    outer = []
    for i in range(29):
        a = math.radians(90.0 * i / 28)
        e = 2.0 / BOTTOM_P
        outer.append((math.sin(a) ** e, BOTTOM_H * (1.0 - math.cos(a) ** e)))
    outer += line((1.0, BOTTOM_H), (1.0, SHOULDER_Z), 24)[1:]
    outer += blend((1.0, SHOULDER_Z), (NECK_R, NECK_Z), 32)[1:]
    outer += line((NECK_R, NECK_Z), (NECK_R, LIP_Z - 0.02), 2)[1:]
    inner = offset_inward(outer, GLASS_T)

    # the lip: a thick glass roll round the neck, its top rounded over into the mouth
    roll = LIP_H * 0.5
    top_round = 0.05
    lip = arc(NECK_R + 0.02, LIP_Z - 0.02, 0.02, 180, 90, 4)[1:]
    lip += line(lip[-1], (LIP_R - roll, LIP_Z), 2)[1:]
    lip += arc(LIP_R - roll, LIP_Z + roll, roll, -90, 90, 16)[1:]
    lip += line(lip[-1], (MOUTH_R + top_round, TOP_Z), 4)[1:]
    lip += arc(MOUTH_R + top_round, TOP_Z - top_round, top_round, 90, 180, 8)[1:]
    lip += line(lip[-1], inner[-1], 6)[1:]
    return outer + lip + inner[::-1][1:]


def cork_profile():
    """Solid (r, z) profile of a tapered cork with softly rounded edges."""
    top_edge, bottom_edge = 0.06, 0.025
    pts = line((0.0, CORK_TOP), (CORK_R1 - top_edge, CORK_TOP), 6)
    pts += arc(CORK_R1 - top_edge, CORK_TOP - top_edge, top_edge, 90, 0, 8)[1:]
    pts += line(pts[-1], (CORK_R0, CORK_BOTTOM + bottom_edge), 12)[1:]
    pts += arc(CORK_R0 - bottom_edge, CORK_BOTTOM + bottom_edge, bottom_edge, 0, -90, 4)[1:]
    pts += line(pts[-1], (0.0, CORK_BOTTOM), 6)[1:]
    return pts


def cork_material():
    """Cork: warm tan with darker grain and the pits cork is known by, slightly bumpy."""
    mat = bpy.data.materials.new("Cork")
    mat.use_nodes = True
    nt = mat.node_tree
    bsdf = nt.nodes["Principled BSDF"]
    bsdf.inputs["Roughness"].default_value = 0.78

    coord = nt.nodes.new("ShaderNodeTexCoord")
    grain = nt.nodes.new("ShaderNodeTexNoise")
    grain.inputs["Scale"].default_value = 26.0
    grain.inputs["Detail"].default_value = 8.0
    grain.inputs["Roughness"].default_value = 0.6
    nt.links.new(coord.outputs["Object"], grain.inputs["Vector"])
    ramp = nt.nodes.new("ShaderNodeValToRGB")
    ramp.color_ramp.elements[0].position = 0.35
    ramp.color_ramp.elements[0].color = (0.36, 0.17, 0.06, 1.0)
    ramp.color_ramp.elements[1].position = 0.65
    ramp.color_ramp.elements[1].color = (0.68, 0.42, 0.20, 1.0)
    nt.links.new(grain.outputs["Fac"], ramp.inputs["Fac"])

    pits = nt.nodes.new("ShaderNodeTexVoronoi")
    pits.inputs["Scale"].default_value = 34.0
    pits.inputs["Randomness"].default_value = 1.0
    nt.links.new(coord.outputs["Object"], pits.inputs["Vector"])
    pit_mask = nt.nodes.new("ShaderNodeMapRange")
    pit_mask.inputs["From Min"].default_value = 0.10
    pit_mask.inputs["From Max"].default_value = 0.04
    nt.links.new(pits.outputs["Distance"], pit_mask.inputs["Value"])
    mix = nt.nodes.new("ShaderNodeMix")
    mix.data_type = "RGBA"
    # the mix node has a socket of each type under the same name; pick the colour ones
    color_in = [s for s in mix.inputs if s.type == "RGBA"]
    color_in[1].default_value = (0.13, 0.055, 0.02, 1.0)
    nt.links.new(pit_mask.outputs["Result"], mix.inputs[0])
    nt.links.new(ramp.outputs["Color"], color_in[0])
    nt.links.new([s for s in mix.outputs if s.type == "RGBA"][0], bsdf.inputs["Base Color"])

    bump = nt.nodes.new("ShaderNodeBump")
    bump.inputs["Strength"].default_value = 0.35
    bump.inputs["Distance"].default_value = 0.02
    height = nt.nodes.new("ShaderNodeMath")
    height.operation = "SUBTRACT"
    nt.links.new(grain.outputs["Fac"], height.inputs[0])
    nt.links.new(pit_mask.outputs["Result"], height.inputs[1])
    nt.links.new(height.outputs["Value"], bump.inputs["Height"])
    nt.links.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    return mat


def holdout_material():
    mat = bpy.data.materials.new("Holdout")
    mat.use_nodes = True
    nodes = mat.node_tree.nodes
    nodes.clear()
    hold = nodes.new("ShaderNodeHoldout")
    out = nodes.new("ShaderNodeOutputMaterial")
    mat.node_tree.links.new(hold.outputs[0], out.inputs[0])
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
    """Emissive shell round the whole scene, seen by the camera and through the glass but by nothing else. A shell
    rather than a wall: rays the shoulder bends upward would otherwise miss it and matte the sky in as glass."""
    bpy.ops.mesh.primitive_uv_sphere_add(radius=45.0, segments=32, ring_count=16, location=(0.0, 0.0, TOP_Z * 0.5))
    wall = bpy.context.active_object
    wall.name = "Backdrop"
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


# Tube.png canvas: 696 px wide, drawn 2 units wide in Unity (PPU 348). The bottle is 606 px wide; the rows above it
# leave room for the cap and the height follows from the bottle.
CANVAS_W = 696
BOTTLE_W_PX = 606
# the cork reaches above the lip by its height over it and, seen from above, by how far its back edge sits behind
# the back of the lip's flat top
TOP_PX = int(math.ceil(max(0.0, (CORK_TOP - TOP_Z) * math.cos(math.radians(TILT_DEG)) +
                            (CORK_R1 - (LIP_R - LIP_H * 0.5)) * math.sin(math.radians(TILT_DEG))) *
                       BOTTLE_W_PX * 0.5)) + 6
BOTTOM_MARGIN_PX = 24
UNITY_PPU = CANVAS_W / 2.0


def tube_scene():
    """The glass tube under the highlight cards, with the camera fitted to the Tube.png canvas."""
    reset_scene()
    # a dim, cool surround: the glass edges pick it up as a tinted line instead of a white outline
    set_world((0.42, 0.55, 0.85), 0.45)
    tube = revolve("Tube", tube_profile(), thickness=0.0, steps=192)
    tube.data.materials.append(glass_material())

    mid = TOP_Z * 0.5
    # softbox strips -> crisp highlight bands: a strong one on the left, a thin one on the right
    emissive_card("KeyBand", (2.2, 9.0), (-4.5, -6.0, mid), (0, 0, mid), 2.5)
    emissive_card("KeyStrip", (0.35, 9.0), (-4.2, -6.4, mid), (0, 0, mid), 9.0)
    emissive_card("FillStrip", (0.25, 9.0), (5.0, -5.0, mid), (0, 0, mid), 3.0)
    emissive_card("TopCard", (6.0, 6.0), (0.0, -2.0, TOP_Z + 6.0), (0, 0, TOP_Z), 1.5)

    cam = make_camera()
    # the width sets the scale; the canvas is then as tall as the bottle needs
    _, span = fit_camera(cam, tube, (CANVAS_W, 4096), 0, 4096, target_z=mid, max_width_px=BOTTLE_W_PX)
    height = int(math.ceil(TOP_PX + span + BOTTOM_MARGIN_PX))
    fit_camera(cam, tube, (CANVAS_W, height), TOP_PX, height - BOTTOM_MARGIN_PX, target_z=mid,
               max_width_px=BOTTLE_W_PX)
    return tube, cam


def project(cam, p):
    """Canvas pixel (column, row from the top) of a world point."""
    from bpy_extras.object_utils import world_to_camera_view
    scene = bpy.context.scene
    v = world_to_camera_view(scene, cam, Vector(p))
    return v.x * scene.render.resolution_x, (1.0 - v.y) * scene.render.resolution_y


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
    report_slicing(cam, alpha.shape[0])
    report_interior(alpha, LIQUID_GAP)


# Gap between the liquid and the outside of the glass, the same along the walls and under the bottom (Unity units).
LIQUID_GAP = 0.065
EDGE_ALPHA = 0.25  # where the glass edge reads as the edge


def report_slicing(cam, height):
    """9-slice borders for Tube.png: everything that is not plain straight wall stays out of the stretched centre.
    The back of the bottom curve shows through the glass above its front, the front of the shoulder below it."""
    low = project(cam, (0.0, 1.0, BOTTOM_H))[1]
    high = project(cam, (0.0, -1.0, SHOULDER_Z))[1]
    margin = 30
    print(f"[assets] Tube.png {CANVAS_W}x{height}, PPU {UNITY_PPU:g}: spriteBorder bottom (y) "
          f"{int(height - low + margin)}, top (w) {int(high + margin)}")


def report_interior(alpha, gap):
    """Fits the TubeView liquid outline to the glass silhouette shrunk by `gap` all round. Heights are measured
    from the sprite's bottom edge (bottom) or top edge (shoulder, neck, lip), which the 9-slice keeps in place."""
    h, w = alpha.shape
    mask = alpha > EDGE_ALPHA
    cx = w * 0.5

    # straight wall: half width of the silhouette rows between the bottom curve and the shoulder
    cam = bpy.context.scene.camera
    r0 = int(project(cam, (0.0, -1.0, SHOULDER_Z))[1]) - 20
    r1 = int(project(cam, (0.0, 1.0, BOTTOM_H))[1]) + 20
    half = []
    for row in range(r0, r1, 4):
        cols = np.nonzero(mask[row])[0]
        half.append((cols.max() + 1 - cols.min()) * 0.5)
    body = float(np.median(half)) / UNITY_PPU - gap

    # bottom: the lowest silhouette pixel of each column, in units above the sprite's bottom edge, moved inward
    pts = []
    for col in range(w):
        rows = np.nonzero(mask[:, col])[0]
        if len(rows):
            pts.append(((col + 0.5 - cx) / UNITY_PPU, (h - rows.max() - 1) / UNITY_PPU))
    pts = np.array(pts)
    k = 6
    inner = []
    for i in range(k, len(pts) - k):
        tx, ty = pts[i + k] - pts[i - k]
        n = math.hypot(tx, ty)
        inner.append((pts[i][0] - ty / n * gap, pts[i][1] + tx / n * gap))
    inner = np.array(inner)
    inner = inner[np.abs(inner[:, 0]) < body * 0.97]
    y0 = float(inner[:, 1].min())
    u = np.abs(inner[:, 0]) / body
    best = None
    for p in np.arange(1.6, 4.01, 0.05):
        for c in np.arange(0.1, 1.2, 0.005):
            y = y0 + c - c * np.power(np.clip(1.0 - u ** p, 0.0, 1.0), 1.0 / p)
            err = float(np.mean((y - inner[:, 1]) ** 2))
            if best is None or err < best[0]:
                best = (err, p, c)

    # shoulder, neck and the top of the mouth, in units below the top edge
    neck = (project(cam, (NECK_R, 0.0, NECK_Z))[0] - project(cam, (0.0, 0.0, NECK_Z))[0]) / UNITY_PPU
    shoulder = project(cam, (1.0, 0.0, SHOULDER_Z))[1] / UNITY_PPU
    neck_top = project(cam, (NECK_R, 0.0, NECK_Z))[1] / UNITY_PPU
    lip = project(cam, (0.0, 0.0, TOP_Z - 0.05))[1] / UNITY_PPU
    print(f"[assets] liquid (gap {gap}): bodyHalfWidth {body:.3f} neckHalfWidth {neck - gap:.3f} "
          f"bottomInset {y0:.3f} bottomCurveHeight {best[2]:.3f} bottomPower {best[1]:.2f} "
          f"(rms {math.sqrt(best[0]) * UNITY_PPU:.2f} px) shoulderDrop {shoulder:.3f} neckDrop {neck_top:.3f} "
          f"lipDrop {lip:.3f}")

    # tube height at which `slots` one-unit slots stop just under the shoulder: the rounded bottom holds less than
    # a full-width slab of its height, so the liquid sits that much higher
    p, c = best[1], best[2]
    uu = np.linspace(0.0, 1.0, 20001)
    deficit = c * (1.0 - float(np.mean(np.power(1.0 - uu ** p, 1.0 / p))))
    slots = 3
    print(f"[assets] tube height for {slots} slots: {y0 + slots + deficit + LIQUID_HEADROOM + shoulder:.3f}")


LIQUID_HEADROOM = 0.06  # space left between a full tube's liquid and the start of the shoulder


CORK_SEEN_THROUGH_GLASS = 0.55  # how much of the cork shows through the lip and neck in front of it


def load_png(path):
    img = bpy.data.images.load(path)
    w, h = img.size
    px = np.array(img.pixels[:], dtype=np.float32).reshape(h, w, 4)[::-1]
    bpy.data.images.remove(img)
    return px


def render_cap():
    """The cork seen through the tube camera, cropped to the rows it covers so its top edge is the top of
    Tube.png: placed with a top-center pivot at the top of the tube sprite, it sits in the mouth.
    Cap.png is the whole cork, for its flight. CapSeated.png is the cork pushed in: the part the lip and neck
    hide is faded, and it is drawn over the glass, whose highlights then show on that part."""
    tube, cam = tube_scene()
    set_world((0.95, 0.93, 0.90), 0.25)
    key = bpy.data.lights.new("Key", "AREA")
    key.energy = 320
    key.size = 5.0
    key_obj = bpy.data.objects.new("Key", key)
    key_obj.location = (-4.0, -5.0, TOP_Z + 5.0)
    key_obj.rotation_euler = (Vector((0, 0, TOP_Z)) - key_obj.location).to_track_quat("-Z", "Y").to_euler()
    bpy.context.scene.collection.objects.link(key_obj)

    cork = revolve("Cork", cork_profile(), thickness=0.0, steps=128)
    cork.data.materials.append(cork_material())

    # the glass only cuts the cork out where it stands in front of it, nothing else sees it
    tube.data.materials.clear()
    tube.data.materials.append(holdout_material())
    for attr in ("visible_diffuse", "visible_glossy", "visible_transmission", "visible_volume_scatter",
                 "visible_shadow"):
        setattr(tube, attr, False)

    tube.hide_render = True
    render(os.path.join(OUT, "cork_whole.png"))
    whole = load_png(os.path.join(OUT, "cork_whole.png"))
    tube.hide_render = False
    render(os.path.join(OUT, "cork_cut.png"))
    cut = load_png(os.path.join(OUT, "cork_cut.png"))

    rows = np.nonzero(whole[..., 3].max(axis=1) > 0.0)[0]
    bottom = int(rows.max()) + 4
    print(f"[assets] cork covers rows {int(rows.min())}..{int(rows.max())} of {whole.shape[0]}")
    seated = whole.copy()
    hidden = whole[..., 3] - cut[..., 3]
    seated[..., 3] = cut[..., 3] + hidden * CORK_SEEN_THROUGH_GLASS
    # pixels come back as stored, already sRGB
    save_rgba(os.path.join(OUT, "Cap.png"), whole[:bottom])
    save_rgba(os.path.join(OUT, "CapSeated.png"), seated[:bottom])


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
