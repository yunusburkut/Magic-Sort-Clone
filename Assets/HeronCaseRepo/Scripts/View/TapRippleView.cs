using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// A ring opens under the finger on every tap, anywhere on the screen, so the player always sees the touch landed,
// even on a busy tube or on empty table. One hand-emitted particle per tap; TapRipple.shader grows, thins and fades
// the ring from the particle's age, so a resting screen costs nothing but the input check.
[RequireComponent(typeof(Camera))]
public sealed class TapRippleView : MonoBehaviour
{
    // the shader reads the age from TEXCOORD0.z
    private static readonly List<ParticleSystemVertexStream> AgeStreams = new List<ParticleSystemVertexStream>
    {
        ParticleSystemVertexStream.Position,
        ParticleSystemVertexStream.Color,
        ParticleSystemVertexStream.UV,
        ParticleSystemVertexStream.AgePercent,
    };

    [SerializeField] private Material rippleMaterial;
    // over the tubes, even one that pours with its sorting boost, and over the UI canvas
    [SerializeField] private int sortingOrder = 1000;
    // the ring at its widest, as a fraction of the screen height, so it is the same size on every screen and zoom
    [SerializeField] private float size = 0.1f;
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private Color color = new Color(1f, 1f, 1f, 0.6f);

    private Camera _camera;
    private ParticleSystem _ripples;

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        _ripples = CreateSystem();
    }

    // Reads the screen directly instead of the event system, so taps that hit nothing show as well.
    private void Update()
    {
        var touched = false;
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touches = touchscreen.touches;
            for (var i = 0; i < touches.Count; i++)
            {
                if (!touches[i].press.wasPressedThisFrame) continue;
                Play(touches[i].position.ReadValue());
                touched = true;
            }
        }

        // with touches simulated from the mouse one click is both, and gets one ring
        if (touched) return;
        var mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) Play(mouse.position.ReadValue());
    }

    public void Play(Vector2 screenPosition)
    {
        // the board lies on z = 0
        var depth = -_camera.transform.position.z;
        var world = _camera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, depth));
        world.z = 0f;

        var ripple = new ParticleSystem.EmitParams();
        ripple.position = world;
        ripple.velocity = Vector3.zero;
        ripple.startSize = size * 2f * _camera.orthographicSize;
        ripple.startLifetime = duration;
        ripple.startColor = color;

        if (!_ripples.isPlaying) _ripples.Play();
        _ripples.Emit(ripple, 1);
    }

    private ParticleSystem CreateSystem()
    {
        var go = new GameObject("TapRipples");
        go.transform.SetParent(transform, false);
        var ps = go.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.playOnAwake = false;
        // not looping, so it stops by itself once the last ring is gone; Play wakes it again
        main.loop = false;
        main.duration = 1f;
        main.maxParticles = 8;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.enabled = false;
        var shape = ps.shape;
        shape.enabled = false;

        var psRenderer = go.GetComponent<ParticleSystemRenderer>();
        psRenderer.sharedMaterial = rippleMaterial;
        psRenderer.sortingOrder = sortingOrder;
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.SetActiveVertexStreams(AgeStreams);

        return ps;
    }
}
