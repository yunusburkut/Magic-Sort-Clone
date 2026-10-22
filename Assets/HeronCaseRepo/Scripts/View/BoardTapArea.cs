using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

// Takes the taps that miss the bottles. Each bottle owns the cell around its resting spot, as wide as the spacing to
// its neighbors and as tall as the spacing between rows, so a tap in the gap still picks the bottle beside it, and a
// bottle that is lifted or away pouring keeps its cell at home. A tap outside every cell is a tap on empty space.
// The area sits at the back of the camera's view: the bottles' own colliders and the UI canvas are nearer, so they
// take a tap first and the full-screen buttons keep working.
[RequireComponent(typeof(BoxCollider2D))]
public sealed class BoardTapArea : MonoBehaviour, IPointerClickHandler
{
    // wider than any screen the board is shown on
    private const float Extent = 10000f;
    // how far into the camera's view range it sits
    private const float Depth = 0.9f;

    private List<TubeView> _tubes;
    private Vector2 _cellHalf;

    public void Setup(Camera cam, Vector2 cellSize)
    {
        var camPos = cam.transform.position;
        transform.position = new Vector3(camPos.x, camPos.y, camPos.z + cam.farClipPlane * Depth);
        GetComponent<BoxCollider2D>().size = new Vector2(Extent, Extent);
        _cellHalf = cellSize * 0.5f;
    }

    public void SetTubes(List<TubeView> tubes)
    {
        _tubes = tubes;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        var tube = TubeAt(eventData.pointerPressRaycast.worldPosition);
        if (tube != null)
        {
            EventBus<TubeClickedEvent>.Publish(new TubeClickedEvent { Tube = tube });
            return;
        }

        EventBus<EmptySpaceClickedEvent>.Publish(new EmptySpaceClickedEvent());
    }

    // a cloaked or solved bottle takes no taps, so its cell counts as empty space
    private TubeView TubeAt(Vector3 point)
    {
        if (_tubes == null) return null;

        for (var i = 0; i < _tubes.Count; i++)
        {
            var tube = _tubes[i];
            if (!tube.IsTappable) continue;

            var offset = point - tube.RestWorldPosition;
            if (Mathf.Abs(offset.x) <= _cellHalf.x && Mathf.Abs(offset.y) <= _cellHalf.y) return tube;
        }

        return null;
    }
}
