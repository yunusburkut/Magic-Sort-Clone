using System.Collections.Generic;
using UnityEngine;

// Slot logic of one tube. The visible liquid lives in TubeLiquid: spawning and reveals are mirrored there right away,
// while pours update the logic at once and let the pour animation move the visible volume.
public sealed class TubeWaterSlots
{
    private sealed class WaterSegment
    {
        public Color Color;
        public int Count;
        public bool IsHidden;
    }

    private readonly List<WaterSegment> _segments = new List<WaterSegment>();
    private readonly TubeLiquid _liquid;
    private readonly int _capacity;
    private int _totalCount;

    public bool IsFull => _totalCount >= _capacity;
    public bool IsEmpty => _totalCount == 0;
    public int AvailableSlots => _capacity - _totalCount;

    public Color TopColor => _segments.Count > 0 ? _segments[_segments.Count - 1].Color : Color.clear;
    public int TopColorCount => _segments.Count > 0 ? _segments[_segments.Count - 1].Count : 0;
    public bool IsSingleColor => _segments.Count == 1;

    public TubeWaterSlots(TubeLiquid liquid, int capacity)
    {
        _liquid = liquid;
        _capacity = capacity;
    }

    public bool HasColor(Color color)
    {
        for (var i = 0; i < _segments.Count; i++)
        {
            if (_segments[i].Color == color) return true;
        }
        return false;
    }

    public void SpawnWater(Color color, bool isHidden)
    {
        _totalCount++;
        _liquid.Append(color, 1f, isHidden);

        if (_segments.Count > 0)
        {
            var top = _segments[_segments.Count - 1];
            if (top.Color == color && top.IsHidden == isHidden)
            {
                top.Count++;
                return;
            }
        }

        _segments.Add(new WaterSegment { Color = color, Count = 1, IsHidden = isHidden });
    }

    public void RevealTopWater()
    {
        if (_segments.Count == 0) return;
        _segments[_segments.Count - 1].IsHidden = false;
        _liquid.RevealTop(true);
    }

    public void AddWaters(Color color, int count)
    {
        _totalCount += count;

        if (_segments.Count > 0)
        {
            var top = _segments[_segments.Count - 1];
            if (top.Color == color && !top.IsHidden)
            {
                top.Count += count;
                return;
            }
        }

        _segments.Add(new WaterSegment { Color = color, Count = count });
    }

    public void RemoveTopWaters(int count)
    {
        if (_segments.Count == 0 || count <= 0) return;

        var top = _segments[_segments.Count - 1];
        _totalCount -= count;

        if (top.Count > count)
        {
            top.Count -= count;
            return;
        }

        _segments.RemoveAt(_segments.Count - 1);
        // TubeLiquid reveals its own new top once the drain animation empties the layer
        if (_segments.Count > 0) _segments[_segments.Count - 1].IsHidden = false;
    }
}
