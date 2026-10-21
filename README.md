# 💧 Magic Sort Clone

> A Water Sort puzzle clone built in **Unity 6**, focused on **game feel** over feature breadth.  
> Every animation, haptic pulse, and particle burst exists to make the player *feel* smart when they solve a tube.

## ⚙️ Setup

The project needs **[Feel](https://assetstore.unity.com/packages/tools/particles-effects/feel-183370)** by More Mountains (v6.1 or later), a paid Asset Store package that is **not** included in this repository. Without it the scripts do not compile.

1. Buy or claim Feel on the Asset Store.
2. Open the project in Unity 6 and import Feel through **Window → Package Manager → My Assets**, so it lands in `Assets/Feel`.
3. Only the `MMFeedbacks`, `MMTools` and `NiceVibrations` folders are used; the demos can be left out.

`Assets/Feel/` is git-ignored so the package is never committed by accident.

---

## 🎯 Focus Areas

### 🏗️ Clean Architecture
The project is wired together through a manual **composition root** (`GameBootstrapper`) with no DI framework — controllers, services, and views are injected explicitly. A lightweight **EventBus** decouples layers without creating hard dependencies between them.

Layers follow a strict one-way dependency rule:

```
Data → Services → Controller → View
```

### 🎬 Game Feel — The Real Challenge
Game feel was the primary design constraint, not the puzzle logic. Every interaction point has a dedicated animation pass:

| Moment | Technique |
|---|---|
| Tube select | Anticipation dip → lift with `OutBack` ease |
| Invalid move | Procedural shake with **exponential decay** |
| Pour arc | Two-phase arc path (`OutSine` → `InSine`) + tilt with `OutBack` |
| Tube solved | **Squash & Stretch** → soft spring settle |
| Level enter | Staggered slide-in from off-screen with `OutBack` |
| Cloak reveal | Anticipation squash → stretch → launch (full 12-principle sequence) |
| Fill VFX | Particle color matched to solved tube color at runtime |

All animation parameters live in a single `GameSettings` **ScriptableObject**, making iteration and tuning zero-risk.

### 📐 Disney's 12 Principles of Animation
Applied deliberately, not as an afterthought:

- **Anticipation** — Tube dips slightly *before* lifting on select; cloak squashes before launching.
- **Squash & Stretch** — Solved tube deforms on completion, then settles on a spring ease baked from Feel's damped spring solver: one soft overshoot instead of an elastic buzz.
- **Follow-Through** — Pour tilt returns with `OutBack` overshoot; deselect returns with `OutBounce`.
- **Slow In / Slow Out** — All motion uses `OutSine` / `InSine` for organic feel, never linear.
- **Staging** — Tubes enter one by one with a stagger delay so the player reads the board layout.
- **Secondary Action** — Pour line fades in on the target tube during the pour hold, then fades out independently.
- **Timing** — A `QueuedPourSpeedMultiplier` accelerates back-to-back pours to maintain momentum.

### 📳 Platform-Aware Haptics
`HapticService` plays Feel's **Nice Vibrations** presets, which map to Core Haptics on iOS and amplitude-controlled vibration on Android:

- **Select** — `Selection` tick on tube pick-up.
- **Invalid move** — `Warning`, in time with the shake.
- **Pour lands** — `SoftImpact` when the stream reaches the target.
- **Tube solved** — `Success`.

### ⚡ Responsive Input
Animations never swallow a tap. The target tube is free as soon as its liquid has landed, without waiting for the source to fly home, and a tap on a tube that is still busy is buffered for `InputBufferWindow` seconds and replayed the moment it frees up.

### 🧹 Memory & Tween Hygiene
A `TweenScope` wrapper tracks all active tweens per-view and kills them on `OnDestroy`, eliminating the most common DOTween leak pattern. All `TweenCallback` delegates are cached on `Awake` — no per-frame allocations in the hot path.

---

## 🛠️ Tech Stack

- **Unity 6** · **C#**
- **DOTween Pro** — All animation sequencing
- **Feel (More Mountains)** — Spring solver for eases and camera zoom, Nice Vibrations haptics (not included, see Setup)
- **ScriptableObject-based config** — Designer-friendly tuning

---

## 🧠 Key Takeaways

- Game feel is an **engineering discipline**, not a polish pass — it needs to be designed in from day one.
- The 12 principles of animation translate directly to tween easing choices and sequence structure.
- A single `GameSettings` ScriptableObject as the source of truth makes rapid iteration possible without code changes.
- Haptics are a silent feedback channel that meaningfully increases perceived quality on mobile.
- Clean architecture pays off immediately — swapping the tube interaction controller (`ITubeInteractionController`) requires zero changes to the view layer.
