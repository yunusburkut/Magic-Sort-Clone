#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Editor-only capture run, started by ClaudeCaptureTrigger: opens the level, plays one pour and saves frames
// plus any warnings and errors to Logs/ClaudeCaptures, then leaves play mode.
[DefaultExecutionOrder(-32000)]
public sealed class DevPourCapture : MonoBehaviour
{
    public const string ActiveKey = "ClaudeCapture.Active";
    private const int CaptureFps = 30;
    private const int CaptureHeight = 960;

    // capturing at the end of the frame sees LateUpdate's work, like the stream mesh
    private static readonly WaitForEndOfFrame s_endOfFrame = new WaitForEndOfFrame();

    private readonly StringBuilder _log = new StringBuilder();
    private RenderTexture _rt;
    private Texture2D _readback;
    // looked up once in Start; the ring sits on the camera next to it
    private Camera _camera;
    private TapRippleView _ripple;

    public static string OutDir => Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs", "ClaudeCaptures"));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        if (!SessionState.GetBool(ActiveKey, false)) return;
        SessionState.SetBool(ActiveKey, false);

        // fixed steps and a fixed seed from the very first frame, so two runs give the same frames
        Time.captureFramerate = CaptureFps;
        UnityEngine.Random.InitState(7);

        var go = new GameObject("DevPourCapture");
        DontDestroyOnLoad(go);
        go.AddComponent<DevPourCapture>();
    }

    // particle systems pick a new seed on Play unless told otherwise; hierarchy order keeps the seeds stable
    private void FixParticleSeeds()
    {
        var systems = Collect<ParticleSystem>(true);
        var n = 0;
        foreach (var ps in systems)
        {
            if (ps.isPlaying) continue;
            ps.useAutoRandomSeed = false;
            ps.randomSeed = (uint)(++n);
        }
        _log.AppendLine($"Seeded {n} of {systems.Count} particle systems");
    }

    private static readonly string[] s_benchStats =
    {
        "BehaviourUpdate", "LateBehaviourUpdate", "PreLateUpdate.ParticleSystemBeginUpdateAll",
        "SetPass Calls Count", "Standard Draw Calls Count", "SRP Batcher Draw Calls Count", "Dynamic Batches Count",
        "Dynamic Batched Draw Calls Count", "Sprites rendered", "Render Textures Changes Count",
        "PlayerLoop", "RenderPlayModeViewCameras", "CPU Render Thread Frame Time", "Gfx.WaitForRenderThread",
        "Triangles Count", "Vertices Count",
    };

    // iPhone 11 in landscape, the way the board is laid out
    private const float PhoneAspect = 1792f / 828f;

    private IEnumerator CoverageAfter(int frames, string label)
    {
        yield return Frames(frames);
        yield return s_endOfFrame;
        Coverage(label);
    }

    // How many screens' worth of pixels each shader shades in one frame (overdraw), at the phone's aspect.
    private void Coverage(string label)
    {
        var cam = _camera;
        var halfH = cam.orthographicSize;
        var halfW = halfH * PhoneAspect;
        var c = cam.transform.position;
        var view = new Rect(c.x - halfW, c.y - halfH, halfW * 2f, halfH * 2f);
        var viewArea = (double)view.width * view.height;
        var byShader = new SortedDictionary<string, double[]>();

        foreach (var r in Collect<Renderer>(false))
        {
            if (!r.enabled || !r.gameObject.activeInHierarchy || r.sharedMaterial == null) continue;
            if ((cam.cullingMask & (1 << r.gameObject.layer)) == 0) continue;
            if (!Overlaps(r.bounds, view)) continue;
            double area;
            if (r is ParticleSystemRenderer) area = ParticleArea(r.GetComponent<ParticleSystem>());
            else if (r is SpriteRenderer sr && sr.drawMode == SpriteDrawMode.Simple && sr.sprite != null)
                area = TriangleArea(r.transform, sr.sprite.vertices, sr.sprite.triangles);
            else if (r.TryGetComponent<MeshFilter>(out var mf) && mf.sharedMesh != null)
                area = TriangleArea(r.transform, mf.sharedMesh.vertices, mf.sharedMesh.triangles);
            else area = ClippedArea(r.bounds, view);
            Add(byShader, r.sharedMaterial.shader.name, area / viewArea);
        }

        foreach (var g in Collect<Graphic>(false))
        {
            if (!g.isActiveAndEnabled || g.canvas == null || g.color.a <= 0f) continue;
            var corners = new Vector3[4];
            g.rectTransform.GetWorldCorners(corners);
            var w = Vector3.Distance(corners[0], corners[3]);
            var h = Vector3.Distance(corners[0], corners[1]);
            var frac = g.canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? w * h / ((double)Screen.width * Screen.height)
                : w * h / viewArea;
            Add(byShader, "UI/" + (g.material != null ? g.material.shader.name : "?"), frac);
        }

        double total = 0;
        var sb = new StringBuilder();
        foreach (var kv in byShader)
        {
            total += kv.Value[0];
            sb.Append($" | {kv.Key} {kv.Value[0]:0.000}x ({kv.Value[1]:0})");
        }
        _log.AppendLine($"coverage {label} @{PhoneAspect:0.000}: total {total:0.000}x{sb}");
    }

    private static void Add(SortedDictionary<string, double[]> map, string key, double frac)
    {
        if (!map.TryGetValue(key, out var v)) map[key] = v = new double[2];
        v[0] += frac;
        v[1] += 1;
    }

    private static bool Overlaps(Bounds b, Rect view)
    {
        return b.max.x > view.xMin && b.min.x < view.xMax && b.max.y > view.yMin && b.min.y < view.yMax;
    }

    private static double ClippedArea(Bounds b, Rect view)
    {
        var w = Mathf.Min(b.max.x, view.xMax) - Mathf.Max(b.min.x, view.xMin);
        var h = Mathf.Min(b.max.y, view.yMax) - Mathf.Max(b.min.y, view.yMin);
        return w > 0f && h > 0f ? (double)w * h : 0;
    }

    private static double TriangleArea(Transform t, IList<Vector2> local, IList<ushort> tris)
    {
        var area = 0.0;
        for (var i = 0; i + 2 < tris.Count; i += 3)
        {
            Vector2 a = t.TransformPoint(local[tris[i]]), b = t.TransformPoint(local[tris[i + 1]]), d = t.TransformPoint(local[tris[i + 2]]);
            area += Math.Abs((b.x - a.x) * (d.y - a.y) - (d.x - a.x) * (b.y - a.y)) * 0.5;
        }
        return area;
    }

    private static double TriangleArea(Transform t, Vector3[] local, int[] tris)
    {
        var area = 0.0;
        for (var i = 0; i + 2 < tris.Length; i += 3)
        {
            Vector2 a = t.TransformPoint(local[tris[i]]), b = t.TransformPoint(local[tris[i + 1]]), d = t.TransformPoint(local[tris[i + 2]]);
            area += Math.Abs((b.x - a.x) * (d.y - a.y) - (d.x - a.x) * (b.y - a.y)) * 0.5;
        }
        return area;
    }

    private static ParticleSystem.Particle[] s_particles = new ParticleSystem.Particle[256];

    private static double ParticleArea(ParticleSystem ps)
    {
        if (ps == null) return 0;
        var n = ps.GetParticles(s_particles);
        var area = 0.0;
        for (var i = 0; i < n; i++)
        {
            var s = s_particles[i].GetCurrentSize(ps);
            area += (double)s * s;
        }
        return area;
    }

    // game-side main thread work: from the first Update to just before rendering, so the editor's own work is left out
    private readonly System.Diagnostics.Stopwatch _frameWatch = new System.Diagnostics.Stopwatch();
    private long _frameStartBytes;
    private bool _benching;
    private bool _benchSkip;
    private int _benchFrames;
    private long _benchBytes, _benchMaxBytes;
    private double _benchMs, _benchMaxMs;

    private void Update()
    {
        _frameStartBytes = GC.GetAllocatedBytesForCurrentThread();
        _frameWatch.Restart();
    }

    private void OnBeforeRender()
    {
        if (!_benching) return;
        if (_benchSkip)
        {
            _benchSkip = false;
            return;
        }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - _frameStartBytes;
        var ms = _frameWatch.Elapsed.TotalMilliseconds;
        _benchFrames++;
        _benchBytes += bytes;
        _benchMaxBytes = Math.Max(_benchMaxBytes, bytes);
        _benchMs += ms;
        _benchMaxMs = Math.Max(_benchMaxMs, ms);
    }

    private static void DumpMarkers()
    {
        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        var sb = new StringBuilder();
        foreach (var h in handles)
        {
            var d = ProfilerRecorderHandle.GetDescription(h);
            var n = d.Name;
            if (n.Contains("Render") || n.Contains("Camera") || n.Contains("Batch") || n.Contains("Draw") || n.Contains("SetPass")
                || n.Contains("Particle") || n.Contains("Sprite") || n.Contains("Cull") || n.Contains("Volume") || n.Contains("2D"))
            {
                sb.AppendLine($"{d.Category.Name}\t{n}\t{d.UnitType}");
            }
        }
        File.WriteAllText(Path.Combine(OutDir, "markers.txt"), sb.ToString());
    }

    // Averages (and peaks) of a few profiler stats over a stretch of frames with nothing captured.
    private IEnumerator Bench(string label, int frames)
    {
        _benching = true;
        _benchSkip = true;
        _benchFrames = 0;
        _benchBytes = _benchMaxBytes = 0;
        _benchMs = _benchMaxMs = 0;

        var handles = new List<ProfilerRecorderHandle>();
        ProfilerRecorderHandle.GetAvailable(handles);
        var recorders = new ProfilerRecorder[s_benchStats.Length];
        var units = new ProfilerMarkerDataUnit[s_benchStats.Length];
        for (var i = 0; i < s_benchStats.Length; i++)
        {
            foreach (var h in handles)
            {
                var d = ProfilerRecorderHandle.GetDescription(h);
                if (d.Name != s_benchStats[i]) continue;
                units[i] = d.UnitType;
                recorders[i] = new ProfilerRecorder(h, frames + 8);
                if (!recorders[i].IsRunning) recorders[i].Start();
                break;
            }
        }

        yield return Frames(frames);
        _benching = false;

        var f = Math.Max(1, _benchFrames);
        _log.Append($"bench {label} ({_benchFrames}f): game {_benchMs / f:0.000}ms max {_benchMaxMs:0.00} alloc {_benchBytes / f}B max {_benchMaxBytes}");
        for (var i = 0; i < recorders.Length; i++)
        {
            var r = recorders[i];
            if (!r.Valid)
            {
                _log.Append($" | {s_benchStats[i]} n/a");
                continue;
            }
            // the first sample is the partial frame the recorder started in
            double sum = 0, max = 0;
            var count = 0;
            for (var s = 1; s < r.Count; s++)
            {
                double v = r.GetSample(s).Value;
                sum += v;
                max = Math.Max(max, v);
                count++;
            }
            r.Dispose();
            var avg = count > 0 ? sum / count : 0;
            _log.Append(units[i] == ProfilerMarkerDataUnit.TimeNanoseconds
                ? $" | {s_benchStats[i]} {avg / 1e6:0.000}ms max {max / 1e6:0.00}"
                : $" | {s_benchStats[i]} {avg:0.0} max {max:0}");
        }
        _log.AppendLine();
    }

    private void OnEnable()
    {
        Application.onBeforeRender += OnBeforeRender;
        Application.logMessageReceived += OnLog;
        EventBus<PourFlowEndedEvent>.Subscribe(OnPourFlowEnded);
        EventBus<PourAnimationCompletedEvent>.Subscribe(OnPourAnimationCompleted);
    }

    private void OnDisable()
    {
        Application.onBeforeRender -= OnBeforeRender;
        Application.logMessageReceived -= OnLog;
        EventBus<PourFlowEndedEvent>.Unsubscribe(OnPourFlowEnded);
        EventBus<PourAnimationCompletedEvent>.Unsubscribe(OnPourAnimationCompleted);
    }

    private void OnDestroy()
    {
        if (_rt != null) Destroy(_rt);
        if (_readback != null) Destroy(_readback);
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        if (type == LogType.Log) return;
        _log.AppendLine($"[{type}] {message}");
        if (type == LogType.Exception || type == LogType.Error) _log.AppendLine(stackTrace);
    }

    private IEnumerator Start()
    {
        // fixed steps make the frames line up with animation time even while the editor is in the background
        Application.runInBackground = true;
        Time.captureFramerate = CaptureFps;

        Directory.CreateDirectory(OutDir);
        foreach (var file in Directory.GetFiles(OutDir)) File.Delete(file);
        _camera = Camera.main;

        yield return Frames(10);
        ClickFirstButton();
        FixParticleSeeds();
        // real clicks in the Game view while the editor gets focus would select tubes mid-capture
        if (EventSystem.current != null) EventSystem.current.enabled = false;
        // or leave rings; the scripted taps still play theirs
        if (_camera != null && _camera.TryGetComponent(out _ripple)) _ripple.enabled = false;
        for (var i = 0; i < 10; i++)
        {
            yield return Frames(3);
            yield return s_endOfFrame;
            Capture($"enter_{i:00}");
        }

        yield return Frames(25);
        yield return s_endOfFrame;
        Capture("idle");
        yield return Bench("idle", 300);

        if (!TryFindPour(out var from, out var to))
        {
            _log.AppendLine("No valid pour found");
            Finish();
            yield break;
        }

        _log.AppendLine($"Pour {from.name} -> {to.name}");
        Tap(from);
        for (var i = 0; i < 8; i++)
        {
            yield return Frames(1);
            yield return s_endOfFrame;
            Capture($"select_{i:00}");
        }
        yield return s_endOfFrame;
        Capture("selected");
        _pourStartedAt = Time.time;
        Tap(to);

        for (var i = 0; i < 40; i++)
        {
            // every frame through the move over, then every other one
            yield return Frames(i < 12 ? 1 : 2);
            yield return s_endOfFrame;
            Capture($"pour_{i:00}");
        }

        yield return Frames(20);
        yield return s_endOfFrame;
        Capture("after");

        var solved = FindSolvedCandidate(from, to);
        if (solved != null)
        {
            _log.AppendLine("Solved " + solved.name);
            solved.MarkSolved();
            for (var i = 0; i < 40; i++)
            {
                yield return Frames(i < 12 ? 1 : 2);
                yield return s_endOfFrame;
                Capture($"solved_{i:00}");
            }
        }

        // after the first pour and solve: the leftovers of both must cost nothing while the board rests
        yield return Frames(60);
        Coverage("rest");
        yield return Bench("rest", 300);

        if (TryFindPour(out from, out to))
        {
            Tap(from);
            yield return Frames(12);
            Tap(to);
            StartCoroutine(CoverageAfter(15, "pour"));
            yield return Bench("pour", 60);
        }

        yield return Frames(60);
        var next = FindSolvedCandidate(null, null);
        if (next != null)
        {
            next.MarkSolved();
            StartCoroutine(CoverageAfter(10, "solved"));
            yield return Bench("solved", 45);
        }
        Finish();
    }

    // what a finger on the tube does: the click it publishes and the ring it leaves
    private void Tap(TubeView tube)
    {
        if (_ripple != null) _ripple.Play(_camera.WorldToScreenPoint(tube.transform.position));
        EventBus<TubeClickedEvent>.Publish(new TubeClickedEvent { Tube = tube });
    }

    private static IEnumerator Frames(int count)
    {
        for (var i = 0; i < count; i++) yield return null;
    }

    private void ClickFirstButton()
    {
        var buttons = Collect<Button>(false);
        for (var i = 0; i < buttons.Count; i++)
        {
            if (!buttons[i].isActiveAndEnabled || !buttons[i].interactable) continue;
            _log.AppendLine("Clicked " + buttons[i].name);
            buttons[i].onClick.Invoke();
            return;
        }
        _log.AppendLine("No button to click");
    }

    // Prefers a pour onto liquid of the same color, so the splash lands on a surface.
    private bool TryFindPour(out TubeView from, out TubeView to)
    {
        from = null;
        to = null;
        var best = -1;
        var tubes = Collect<TubeView>(false);
        foreach (var a in tubes)
        {
            if (!a.isActiveAndEnabled || a.IsEmpty || a.IsSolved) continue;
            foreach (var b in tubes)
            {
                if (a == b || !b.isActiveAndEnabled || !MoveValidator.CanPour(a, b)) continue;
                var score = (b.IsEmpty ? 0 : 4) + Mathf.Min(a.TopColorCount, b.AvailableSlots);
                if (score <= best) continue;
                best = score;
                from = a;
                to = b;
            }
        }
        return from != null;
    }

    // Any filled tube away from the pour, to play the solved hop and sparks on.
    // the tubes come in the order the level built them, so it is always the same tube
    private TubeView FindSolvedCandidate(TubeView from, TubeView to)
    {
        foreach (var t in Collect<TubeView>(false))
        {
            if (t == from || t == to || !t.isActiveAndEnabled || t.IsEmpty || t.IsSolved) continue;
            return t;
        }
        return null;
    }

    private readonly List<GameObject> _roots = new List<GameObject>();

    // Every T in the open scenes, walked down the hierarchy root by root, so the order is the same on every run.
    private List<T> Collect<T>(bool includeInactive) where T : Component
    {
        var found = new List<T>();
        for (var i = 0; i < SceneManager.sceneCount; i++) CollectFrom(SceneManager.GetSceneAt(i), includeInactive, found);
        // the DontDestroyOnLoad scene is not in that list; this object lives in it
        CollectFrom(gameObject.scene, includeInactive, found);
        return found;
    }

    private void CollectFrom<T>(Scene scene, bool includeInactive, List<T> found) where T : Component
    {
        if (!scene.isLoaded) return;
        scene.GetRootGameObjects(_roots);
        foreach (var root in _roots) found.AddRange(root.GetComponentsInChildren<T>(includeInactive));
    }

    private void Capture(string label)
    {
        var cam = _camera;
        if (cam == null)
        {
            _log.AppendLine("No main camera for " + label);
            return;
        }

        var h = CaptureHeight;
        var w = Mathf.Max(64, Mathf.RoundToInt(h * cam.aspect));
        if (_rt == null || _rt.width != w)
        {
            if (_rt != null) Destroy(_rt);
            if (_readback != null) Destroy(_readback);
            _rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            _readback = new Texture2D(w, h, TextureFormat.RGB24, false);
        }

        var request = new RenderPipeline.StandardRequest { destination = _rt };
        if (RenderPipeline.SupportsRenderRequest(cam, request))
        {
            RenderPipeline.SubmitRenderRequest(cam, request);
        }
        else
        {
            var previous = cam.targetTexture;
            cam.targetTexture = _rt;
            cam.Render();
            cam.targetTexture = previous;
        }

        var active = RenderTexture.active;
        RenderTexture.active = _rt;
        _readback.ReadPixels(new Rect(0, 0, w, h), 0, 0);
        _readback.Apply(false);
        RenderTexture.active = active;

        File.WriteAllBytes(Path.Combine(OutDir, label + ".png"), _readback.EncodeToPNG());
    }


    // Responsiveness probe: taps the target the moment its liquid is in, like a fast player would.
    private float _pourStartedAt;
    private bool _probed;

    private void OnPourFlowEnded(PourFlowEndedEvent e)
    {
        if (_probed) return;
        _probed = true;
        _log.AppendLine($"Flow ended {Time.time - _pourStartedAt:0.00}s after the pour tap; tapping {e.To.name}");
        Tap(e.To);
        StartCoroutine(ReportLift(e.To));
    }

    private IEnumerator ReportLift(TubeView tube)
    {
        var y = tube.transform.localPosition.y;
        yield return Frames(3);
        _log.AppendLine($"{tube.name} rose {tube.transform.localPosition.y - y:0.00} in 0.1s after the tap");
    }

    private void OnPourAnimationCompleted(PourAnimationCompletedEvent e)
    {
        _log.AppendLine($"Source home {Time.time - _pourStartedAt:0.00}s after the pour tap");
    }

    private void Finish()
    {
        DumpMarkers();
        Time.captureFramerate = 0;
        File.WriteAllText(Path.Combine(OutDir, "done.txt"), _log.Length > 0 ? _log.ToString() : "OK");
        EditorApplication.isPlaying = false;
    }
}
#endif
