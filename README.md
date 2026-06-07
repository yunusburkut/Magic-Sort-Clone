# 💧 Magic Sort Clone

> A Water Sort puzzle clone built in **Unity 6**, focused on **game feel** over feature breadth.  
> Every animation, haptic pulse, and particle burst exists to make the player *feel* smart when they solve a tube.

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
| Tube solved | **Squash & Stretch** → elastic bounce |
| Level enter | Staggered slide-in from off-screen with `OutBack` |
| Cloak reveal | Anticipation squash → stretch → launch (full 12-principle sequence) |
| Fill VFX | Particle color matched to solved tube color at runtime |

All animation parameters live in a single `GameSettings` **ScriptableObject**, making iteration and tuning zero-risk.

### 📐 Disney's 12 Principles of Animation
Applied deliberately, not as an afterthought:

- **Anticipation** — Tube dips slightly *before* lifting on select; cloak squashes before launching.
- **Squash & Stretch** — Solved tube deforms on completion (`1.15x / 0.85y`) then rebounds with elastic easing.
- **Follow-Through** — Pour tilt returns with `OutBack` overshoot; deselect returns with `OutBounce`.
- **Slow In / Slow Out** — All motion uses `OutSine` / `InSine` for organic feel, never linear.
- **Staging** — Tubes enter one by one with a stagger delay so the player reads the board layout.
- **Secondary Action** — Pour line fades in on the target tube during the pour hold, then fades out independently.
- **Timing** — A `QueuedPourSpeedMultiplier` accelerates back-to-back pours to maintain momentum.

### 📳 Platform-Aware Haptics
Custom `HapticService` using Android's `VibrationEffect` API (API 26+) and iOS `Handheld.Vibrate`:

- **Select** — Single short pulse (60 ms) on tube pick-up.
- **Win** — Rhythmic pattern: `80ms · pause · 80ms · pause · 600ms` — communicates reward without sound.

All haptic calls are preprocessor-guarded (`#if UNITY_IOS / UNITY_ANDROID`) — zero editor noise.

### 🧹 Memory & Tween Hygiene
A `TweenScope` wrapper tracks all active tweens per-view and kills them on `OnDestroy`, eliminating the most common DOTween leak pattern. All `TweenCallback` delegates are cached on `Awake` — no per-frame allocations in the hot path.

---

## 🛠️ Tech Stack

- **Unity 6** · **C#**
- **DOTween Pro** — All animation sequencing
- **Android VibrationEffect API** — Custom haptic patterns
- **ScriptableObject-based config** — Designer-friendly tuning

---

## 🧠 Key Takeaways

- Game feel is an **engineering discipline**, not a polish pass — it needs to be designed in from day one.
- The 12 principles of animation translate directly to tween easing choices and sequence structure.
- A single `GameSettings` ScriptableObject as the source of truth makes rapid iteration possible without code changes.
- Haptics are a silent feedback channel that meaningfully increases perceived quality on mobile.
- Clean architecture pays off immediately — swapping the tube interaction controller (`ITubeInteractionController`) requires zero changes to the view layer.
