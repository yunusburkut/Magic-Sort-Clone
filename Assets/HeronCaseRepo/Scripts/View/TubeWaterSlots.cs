using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public sealed class TubeWaterSlots
{
    private sealed class WaterSegment
    {
        public Color Color;
        public int Count;
        public int BaseSlot;
        public WaterView View;
        public bool IsHidden;
    }

    private readonly List<WaterSegment> _segments = new List<WaterSegment>();
    private readonly HashSet<Color> _colorSet = new HashSet<Color>();
    private readonly Transform _waterContainer;
    private readonly WaterView _waterPrefab;
    private readonly GameSettings _settings;
    private readonly int _capacity;
    private int _totalCount;

    public float FillAnimEndTime { get; private set; }

    public bool IsFull => _totalCount >= _capacity;
    public bool IsEmpty => _totalCount == 0;
    public int AvailableSlots => _capacity - _totalCount;

    public Color TopColor => _segments.Count > 0 ? _segments[_segments.Count - 1].Color : Color.clear;
    public int TopColorCount => _segments.Count > 0 ? _segments[_segments.Count - 1].Count : 0;
    public bool IsSingleColor => _segments.Count == 1;

    public TubeWaterSlots(Transform waterContainer, WaterView waterPrefab, GameSettings settings, int capacity)
    {
        _waterContainer = waterContainer;
        _waterPrefab = waterPrefab;
        _settings = settings;
        _capacity = capacity;
    }

    public bool HasColor(Color color) => _colorSet.Contains(color);

    public void SpawnWater(Color color, int slotIndex, bool isHidden = false, bool animate = false, float animDelay = 0f)
    {
        _totalCount++;

        if (_segments.Count > 0)
        {
            var top = _segments[_segments.Count - 1];
            if (top.Color == color && top.IsHidden == isHidden)
            {
                top.Count++;
                if (animate)
                {
                    top.View.AnimateHeightTo(SegHeight(top.Count), SegCenterY(top.BaseSlot, top.Count), _settings.WaterRevealDuration, animDelay);
                }
                else
                {
                    top.View.SetHeight(SegHeight(top.Count));
                    top.View.transform.localPosition = new Vector3(0f, SegCenterY(top.BaseSlot, top.Count), 0f);
                }
                return;
            }
        }

        var seg = new WaterSegment { Color = color, Count = 1, BaseSlot = slotIndex, IsHidden = isHidden };
        seg.View = CreateView(color, slotIndex, isHidden);

        if (animate)
        {
            seg.View.SetHeight(0f);
            seg.View.transform.localPosition = new Vector3(0f, SegCenterY(slotIndex, 0), 0f);
            seg.View.AnimateHeightTo(SegHeight(1), SegCenterY(slotIndex, 1), _settings.WaterRevealDuration, animDelay);
        }

        _colorSet.Add(color);
        _segments.Add(seg);
        RefreshSurfaces();
    }

    public void AddWaters(Color color, int count)
    {
        var totalDuration = Mathf.Min(count * _settings.WaterRevealDuration, _settings.PourHoldDuration);
        FillAnimEndTime = Time.time + totalDuration;

        if (_segments.Count > 0 && _segments[_segments.Count - 1].Color == color)
        {
            var top = _segments[_segments.Count - 1];
            top.Count += count;
            _totalCount += count;
            top.View.AnimateHeightTo(SegHeight(top.Count), SegCenterY(top.BaseSlot, top.Count), totalDuration);
            return;
        }

        var slotIndex = _totalCount;
        _totalCount += count;
        var seg = new WaterSegment { Color = color, Count = count, BaseSlot = slotIndex };
        seg.View = CreateView(color, slotIndex, false);
        seg.View.SetHeight(0f);
        seg.View.transform.localPosition = new Vector3(0f, SegCenterY(slotIndex, 0), 0f);
        seg.View.AnimateHeightTo(SegHeight(count), SegCenterY(slotIndex, count), totalDuration);
        _colorSet.Add(color);
        _segments.Add(seg);
        RefreshSurfaces();
    }

    public void RemoveTopWaters(int count)
    {
        if (_segments.Count == 0 || count <= 0) return;

        var top = _segments[_segments.Count - 1];
        _totalCount -= count;
        var totalDuration = Mathf.Min(count * _settings.WaterRevealDuration, _settings.PourHoldDuration);

        if (top.Count > count)
        {
            top.Count -= count;
            top.View.AnimateHeightTo(SegHeight(top.Count), SegCenterY(top.BaseSlot, top.Count), totalDuration);
        }
        else
        {
            _colorSet.Remove(top.Color);
            _segments.RemoveAt(_segments.Count - 1);
            top.View.AnimateHeightTo(0f, SegCenterY(top.BaseSlot, 0), totalDuration)
                .OnComplete(top.View.CachedDestroySelf);
            RevealTopWater();
            RefreshSurfaces();
        }
    }

    public void RevealTopWater()
    {
        if (_segments.Count > 0)
        {
            _segments[_segments.Count - 1].IsHidden = false;
            _segments[_segments.Count - 1].View.SetHidden(false);
        }
    }

    // Only the top segment shows the live surface (ripples + meniscus) in the WaterReveal shader.
    private void RefreshSurfaces()
    {
        var last = _segments.Count - 1;
        for (var i = 0; i <= last; i++)
        {
            _segments[i].View.SetSurface(i == last);
        }
    }

    private WaterView CreateView(Color color, int sortOrder, bool hidden)
    {
        var water = Object.Instantiate(_waterPrefab, _waterContainer);
        water.Init(color);
        if (hidden)
        {
            water.SetHidden(true);
        }
        
        water.SetHeight(SegHeight(1));
        water.transform.localPosition = new Vector3(0f, SegCenterY(sortOrder, 1), 0f);
        water.SetSortingOrder(sortOrder);
#if UNITY_EDITOR
        water.name = $"Water_{sortOrder}";
#endif
        return water;
    }

    private float SegHeight(int count) => count * _settings.WaterSlotHeight + _settings.WaterSegmentOverlap;

    private float SegCenterY(int baseSlot, int count) =>
        (baseSlot + count * 0.5f) * _settings.WaterSlotHeight + _settings.WaterSegmentOverlap * 0.5f;
}
