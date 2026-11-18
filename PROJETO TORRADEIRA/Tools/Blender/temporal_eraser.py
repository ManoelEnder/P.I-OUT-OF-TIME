# Gera o modelo do inimigo TemporalEraser (low-poly, estilo PSX) com esqueleto e animacoes.
# Uso:
#   blender -b --factory-startup -P temporal_eraser.py -- <saida.fbx> [pasta_previews]
# O personagem olha para -Y (frente padrao do Blender) e tem ~1,95 m.
import bpy, bmesh, math, sys
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT = argv[0] if argv else "TemporalEraser.fbx"
PREVIEW = argv[1] if len(argv) > 1 else None

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30

# ---------------- Materiais ----------------

def mat(name, rgb, emit=0.0):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1)
    bsdf.inputs["Roughness"].default_value = 0.9
    if emit > 0:
        bsdf.inputs["Emission Color"].default_value = (*rgb, 1)
        bsdf.inputs["Emission Strength"].default_value = emit
    m.diffuse_color = (*rgb, 1)
    return m

M = {
    "coat": mat("Eraser_Coat", (0.20, 0.19, 0.15)),
    "coatDark": mat("Eraser_CoatDark", (0.11, 0.105, 0.085)),
    "pants": mat("Eraser_Pants", (0.085, 0.07, 0.06)),
    "boots": mat("Eraser_Boots", (0.17, 0.105, 0.06)),
    "sole": mat("Eraser_Sole", (0.05, 0.04, 0.035)),
    "skin": mat("Eraser_Skin", (0.56, 0.52, 0.47)),
    "hair": mat("Eraser_Hair", (0.05, 0.04, 0.035)),
    "belt": mat("Eraser_Belt", (0.13, 0.1, 0.07)),
    "badge": mat("Eraser_Badge", (0.62, 0.55, 0.42)),
    "void": mat("Eraser_FaceVoid", (0.0, 0.0, 0.0)),
    "eyes": mat("Eraser_Eyes", (0.9, 0.95, 1.0), emit=6.0),
}

# ---------------- Esqueleto ----------------

BONES = {
    "root": ((0, 0, 0), (0, 0, 0.2), None),
    "hips": ((0, 0, 0.95), (0, 0, 1.1), "root"),
    "spine": ((0, 0, 1.1), (0, 0, 1.3), "hips"),
    "chest": ((0, 0, 1.3), (0, 0, 1.52), "spine"),
    "neck": ((0, 0, 1.52), (0, -0.02, 1.62), "chest"),
    "head": ((0, -0.02, 1.62), (0, -0.02, 1.9), "neck"),
}
for side, s in (("L", 1), ("R", -1)):
    BONES.update({
        f"shoulder.{side}": ((s * 0.06, 0, 1.5), (s * 0.27, 0, 1.47), "chest"),
        f"upperarm.{side}": ((s * 0.27, 0, 1.47), (s * 0.3, 0.01, 1.16), f"shoulder.{side}"),
        f"forearm.{side}": ((s * 0.3, 0.01, 1.16), (s * 0.31, -0.02, 0.88), f"upperarm.{side}"),
        f"hand.{side}": ((s * 0.31, -0.02, 0.88), (s * 0.31, -0.03, 0.69), f"forearm.{side}"),
        f"thigh.{side}": ((s * 0.1, 0, 0.95), (s * 0.11, 0, 0.52), "hips"),
        f"shin.{side}": ((s * 0.11, 0, 0.52), (s * 0.11, 0.02, 0.1), f"thigh.{side}"),
        f"foot.{side}": ((s * 0.11, 0.02, 0.1), (s * 0.11, -0.14, 0.03), f"shin.{side}"),
    })

arm_data = bpy.data.armatures.new("EraserRig")
rig = bpy.data.objects.new("TemporalEraser", arm_data)
scene.collection.objects.link(rig)
bpy.context.view_layer.objects.active = rig
rig.select_set(True)
bpy.ops.object.mode_set(mode="EDIT")
for name, (h, t, parent) in BONES.items():
    b = arm_data.edit_bones.new(name)
    b.head, b.tail = Vector(h), Vector(t)
for name, (h, t, parent) in BONES.items():
    if parent:
        arm_data.edit_bones[name].parent = arm_data.edit_bones[parent]
        arm_data.edit_bones[name].use_connect = False
bpy.ops.object.mode_set(mode="OBJECT")

# ---------------- Malha ----------------

bm_all = bmesh.new()
deform = bm_all.verts.layers.deform.verify()
group_index = {name: i for i, name in enumerate(BONES)}
mat_index = {k: i for i, k in enumerate(M)}

def add_geom(bm_part, material, weights):
    """Copia uma bmesh para a malha final com material e pesos (funcao pos -> {osso: peso})."""
    vmap = {}
    for v in bm_part.verts:
        nv = bm_all.verts.new(v.co)
        for bone, w in weights(v.co).items():
            nv[deform][group_index[bone]] = w
        vmap[v] = nv
    for f in bm_part.faces:
        try:
            nf = bm_all.faces.new([vmap[v] for v in f.verts])
            nf.material_index = mat_index[material]
        except ValueError:
            pass
    bm_part.free()

def rigid(bone):
    return lambda co: {bone: 1.0}

def box(center, size, material, weights, rot=None):
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    m = Matrix.Diagonal((*size, 1.0))
    if rot:
        m = Matrix.Rotation(math.radians(rot[1]), 4, rot[0]) @ m
    bmesh.ops.transform(bm, matrix=Matrix.Translation(Vector(center)) @ m, verts=bm.verts)
    add_geom(bm, material, weights)

def tube(p0, p1, r0, r1, material, weights, segments=6, squash=1.0):
    p0, p1 = Vector(p0), Vector(p1)
    bm = bmesh.new()
    length = (p1 - p0).length
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments, radius1=r0, radius2=r1, depth=length)
    for v in bm.verts:
        v.co.y *= squash
    direction = (p1 - p0).normalized()
    rot = Vector((0, 0, 1)).rotation_difference(direction).to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=Matrix.Translation((p0 + p1) / 2) @ rot, verts=bm.verts)
    add_geom(bm, material, weights)

def bone_tube(bone, r0, r1, material, segments=6, inset=0.0, squash=1.0):
    h, t, _ = BONES[bone]
    h, t = Vector(h), Vector(t)
    d = (t - h)
    tube(h + d * inset, t, r0, r1, material, rigid(bone), segments, squash)

def smooth(a, b, x):
    t = max(0.0, min(1.0, (x - a) / (b - a)))
    return t * t * (3 - 2 * t)

def torso_weights(co):
    z = co.z
    if z < 1.12:
        return {"hips": 1.0}
    if z < 1.3:
        k = smooth(1.12, 1.3, z)
        return {"hips": 1 - k, "spine": k} if k < 0.5 else {"spine": 1 - k * 0.3, "chest": k * 0.3}
    return {"chest": 1.0}

def skirt_weights(co):
    # Barra do jaleco acompanha a coxa do mesmo lado conforme desce.
    k = smooth(0.98, 0.42, co.z) * 0.75
    side = "L" if co.x >= 0 else "R"
    if abs(co.x) < 0.03:
        return {"hips": 1.0}
    return {"hips": 1 - k, f"thigh.{side}": k}

# Cabeca (curvada para a frente pela pose), cabelo baguncado, rosto de vazio e olhos.
tube((0, -0.02, 1.63), (0, -0.02, 1.89), 0.1, 0.092, "skin", rigid("head"), segments=8, squash=1.1)
tube((0, 0.0, 1.84), (0, 0.0, 1.915), 0.11, 0.07, "hair", rigid("head"), segments=8, squash=1.12)
box((0, 0.065, 1.77), (0.2, 0.1, 0.2), "hair", rigid("head"))
for i, (x, z, r) in enumerate(((-0.09, 1.86, 20), (0.08, 1.88, -25), (0.0, 1.9, 10), (-0.06, 1.83, 40), (0.1, 1.82, -35))):
    box((x, -0.06 + i * 0.03, z), (0.05, 0.08, 0.025), "hair", rigid("head"), rot=("Y", r))
box((0, -0.122, 1.745), (0.15, 0.014, 0.2), "void", rigid("head"))
box((-0.036, -0.131, 1.77), (0.042, 0.006, 0.011), "eyes", rigid("head"))
box((0.036, -0.131, 1.77), (0.042, 0.006, 0.011), "eyes", rigid("head"))
tube((0, -0.0, 1.5), (0, -0.02, 1.64), 0.045, 0.04, "skin", rigid("neck"))

# Tronco do jaleco, ombros caidos, lapelas, cracha e cinto.
tube((0, 0, 0.97), (0, 0, 1.5), 0.2, 0.215, "coat", torso_weights, segments=8, squash=0.62)
box((0, 0, 1.455), (0.56, 0.23, 0.09), "coat", rigid("chest"))
box((-0.065, -0.125, 1.39), (0.075, 0.016, 0.2), "coatDark", rigid("chest"), rot=("Y", -12))
box((0.065, -0.125, 1.39), (0.075, 0.016, 0.2), "coatDark", rigid("chest"), rot=("Y", 12))
box((0.085, -0.14, 1.33), (0.05, 0.012, 0.065), "badge", rigid("chest"))
box((0.085, -0.142, 1.37), (0.012, 0.01, 0.02), "belt", rigid("chest"))
tube((0, 0, 1.0), (0, 0, 1.05), 0.205, 0.205, "belt", rigid("hips"), segments=8, squash=0.66)
box((0, -0.138, 1.025), (0.06, 0.02, 0.05), "sole", rigid("hips"))

# Barra do jaleco: aberta na frente, sem espessura (o material desenha os dois lados).
bm = bmesh.new()
rings = [(1.0, 0.205), (0.74, 0.228), (0.42, 0.255)]
angles = [math.radians(a) for a in range(22, 339, 28)]
grid = []
for z, r in rings:
    row = []
    for a in angles:
        # angulo medido a partir da frente (-Y)
        row.append(bm.verts.new((math.sin(a) * r, -math.cos(a) * r * 0.7, z)))
    grid.append(row)
for i in range(len(rings) - 1):
    for j in range(len(angles) - 1):
        bm.faces.new((grid[i][j], grid[i][j + 1], grid[i + 1][j + 1], grid[i + 1][j]))
add_geom(bm, "coat", skirt_weights)

# Bolsos na barra.
box((-0.125, -0.165, 0.79), (0.1, 0.02, 0.11), "coatDark", skirt_weights)
box((0.125, -0.165, 0.79), (0.1, 0.02, 0.11), "coatDark", skirt_weights)

# Bracos: mangas, punhos e maos compridas.
for side in ("L", "R"):
    bone_tube(f"shoulder.{side}", 0.06, 0.06, "coat")
    bone_tube(f"upperarm.{side}", 0.062, 0.054, "coat")
    bone_tube(f"forearm.{side}", 0.054, 0.05, "coat")
    bone_tube(f"forearm.{side}", 0.058, 0.058, "coatDark", inset=0.85)
    bone_tube(f"hand.{side}", 0.036, 0.022, "skin", segments=4, squash=0.55)

# Pernas e botas.
for side, s in (("L", 1), ("R", -1)):
    bone_tube(f"thigh.{side}", 0.078, 0.062, "pants")
    bone_tube(f"shin.{side}", 0.062, 0.056, "pants")
    box((s * 0.11, -0.04, 0.075), (0.12, 0.25, 0.13), "boots", rigid(f"foot.{side}"))
    box((s * 0.11, -0.045, 0.017), (0.13, 0.27, 0.034), "sole", rigid(f"foot.{side}"))

mesh = bpy.data.meshes.new("EraserMesh")
bm_all.to_mesh(mesh)
bm_all.free()
for m in M.values():
    mesh.materials.append(m)
body = bpy.data.objects.new("EraserBody", mesh)
scene.collection.objects.link(body)
for name in BONES:
    body.vertex_groups.new(name=name)
body.parent = rig
mod = body.modifiers.new("Armature", "ARMATURE")
mod.object = rig

# ---------------- Animacoes ----------------

DEG = math.radians
pb = rig.pose.bones
for b in pb:
    b.rotation_mode = "XYZ"

def key_pose(frame, pose, bob=0.0):
    for b in pb:
        b.rotation_euler = (0, 0, 0)
        b.location = (0, 0, 0)
    for name, (x, y, z) in pose.items():
        pb[name].rotation_euler = (DEG(x), DEG(y), DEG(z))
    pb["root"].location = (0, bob, 0)
    for b in pb:
        b.keyframe_insert("rotation_euler", frame=frame)
    pb["root"].keyframe_insert("location", frame=frame)

def make_action(name, frames, pose_fn):
    action = bpy.data.actions.new(name)
    action.use_fake_user = True
    rig.animation_data_create()
    rig.animation_data.action = action
    for f in range(0, frames + 1):
        pose, bob = pose_fn(f / frames * 2 * math.pi, f)
        key_pose(f, pose, bob)
    for fc in action.fcurves:
        for k in fc.keyframe_points:
            k.interpolation = "LINEAR"
    return action

# Sinais: rotacao X positiva no osso da coxa leva a perna para TRAS; nos ossos do tronco,
# X positivo inclina para a FRENTE (os dois foram conferidos nos previews).
def hunch(extra=0.0):
    return {
        "spine": (10 + extra, 0, 0),
        "chest": (12 + extra * 0.6, 0, 0),
        "neck": (24, 0, 0),
        "head": (14, 0, -8),
        "shoulder.L": (0, 0, -8),
        "shoulder.R": (0, 0, 8),
    }

def idle(phase, f):
    breath = math.sin(phase)
    pose = hunch(breath * 2)
    twitch = 6 if 0.55 < (f / 90) < 0.6 else 0
    pose["head"] = (14 + breath * 2, twitch, -8 - twitch * 2)
    pose["upperarm.L"] = (breath * 2, 0, 3)
    pose["upperarm.R"] = (-breath * 2, 0, -3)
    pose["forearm.L"] = (-6, 0, 0)
    pose["forearm.R"] = (-8, 0, 0)
    pose["hand.L"] = (-4, 0, 0)
    pose["hand.R"] = (-6, 0, 0)
    return pose, breath * 0.006

def gait(stride, knee, lean, arm_swing, arm_fwd, bob_amount, sway):
    def fn(phase, f):
        pose = hunch(lean)
        s = math.sin(phase)
        c = math.cos(phase)
        pose["thigh.L"] = (-stride * s, 0, 0)
        pose["thigh.R"] = (stride * s, 0, 0)
        # O joelho dobra quando a perna passa por baixo do corpo (fase de balanco).
        pose["shin.L"] = (knee * max(0.0, math.sin(phase + 1.2)) ** 1.5, 0, 0)
        pose["shin.R"] = (knee * max(0.0, math.sin(phase + 1.2 + math.pi)) ** 1.5, 0, 0)
        pose["foot.L"] = (-stride * 0.35 * s, 0, 0)
        pose["foot.R"] = (stride * 0.35 * s, 0, 0)
        pose["hips"] = (0, 0, sway * s)
        pose["chest"] = (pose["chest"][0], 0, -sway * 1.4 * s)
        pose["upperarm.L"] = (arm_fwd + arm_swing * s, 0, 4)
        pose["upperarm.R"] = (arm_fwd - arm_swing * s, 0, -4)
        pose["forearm.L"] = (-10 - arm_swing * 0.3 * max(0, s), 0, 0)
        pose["forearm.R"] = (-10 - arm_swing * 0.3 * max(0, -s), 0, 0)
        pose["hand.L"] = (-8 + 6 * c, 0, 0)
        pose["hand.R"] = (-8 - 6 * c, 0, 0)
        pose["head"] = (14 + 3 * abs(c), 0, -8 + 4 * s)
        return pose, -bob_amount * abs(c)
    return fn

idle_action = make_action("Idle", 90, idle)
# Andar arrastado: ciclo de 0,8 s (~2 m/s com o passo dessa perna).
walk_action = make_action("Walk", 24, gait(stride=30, knee=50, lean=4, arm_swing=8, arm_fwd=0, bob_amount=0.03, sway=6))
# Perseguicao: ciclo de 0,53 s (~4 m/s), curvado e com os bracos estendidos para a frente.
run_action = make_action("Run", 16, gait(stride=40, knee=75, lean=18, arm_swing=10, arm_fwd=-55, bob_amount=0.05, sway=8))

rig.animation_data.action = idle_action

# ---------------- Previews (opcional) ----------------

if PREVIEW:
    cam_data = bpy.data.cameras.new("Cam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 2.4
    cam = bpy.data.objects.new("Cam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    sun = bpy.data.objects.new("Sun", bpy.data.lights.new("Sun", "SUN"))
    sun.rotation_euler = (DEG(50), 0, DEG(30))
    scene.collection.objects.link(sun)
    scene.render.engine = "BLENDER_WORKBENCH"
    scene.display.shading.color_type = "MATERIAL"
    scene.render.resolution_x = 360
    scene.render.resolution_y = 420
    views = {"front": ((0, -6, 1.0), (DEG(90), 0, 0)), "side": ((6, 0, 1.0), (DEG(90), 0, DEG(90)))}
    shots = [("Idle", idle_action, 0), ("Walk", walk_action, 0), ("Walk", walk_action, 6), ("Run", run_action, 4)]
    for vname, (loc, rot) in views.items():
        cam.location, cam.rotation_euler = loc, rot
        for clip, action, frame in shots:
            rig.animation_data.action = action
            scene.frame_set(frame)
            scene.render.filepath = f"{PREVIEW}/{vname}_{clip}_{frame:02d}.png"
            bpy.ops.render.render(write_still=True)
    rig.animation_data.action = idle_action
    bpy.data.objects.remove(cam)
    bpy.data.objects.remove(sun)

# ---------------- Exportacao ----------------

bpy.ops.object.select_all(action="DESELECT")
rig.select_set(True)
body.select_set(True)
bpy.context.view_layer.objects.active = rig
bpy.ops.export_scene.fbx(
    filepath=OUT,
    use_selection=True,
    object_types={"ARMATURE", "MESH"},
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    bake_space_transform=False,
    add_leaf_bones=False,
    use_armature_deform_only=True,
    bake_anim=True,
    bake_anim_use_all_actions=True,
    bake_anim_use_nla_strips=False,
    bake_anim_force_startend_keying=True,
    bake_anim_simplify_factor=0.0,
    mesh_smooth_type="FACE",
)
print("EXPORTED", OUT)
