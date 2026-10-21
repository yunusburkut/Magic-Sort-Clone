using UnityEngine;

// Inner cross-section of the bottle in tube-local space: rounded bottom, straight body, sloped shoulder, neck up
// to the lip.
public struct LiquidShape
{
    // Matches BOTTOM_POWER in Liquid.shader: the bottom is a superellipse, between an ellipse and a rounded box.
    public const float BottomPower = 2.1f;
    private const int BottomSegments = 6;
    public const int VertexCount = BottomSegments * 2 + 7;

    public float BodyHalfWidth;
    public float NeckHalfWidth;
    // lowest point of the bottom, in the middle
    public float BottomY;
    // how far up the walls the rounded bottom reaches
    public float BottomCurveHeight;
    public float ShoulderY;
    public float NeckY;
    public float LipY;
    // How tall a horizontal circle of the bottle looks from the slightly-above camera: e = ratio * radius.
    // The camera sees the opening more from above than the bottom, so the ratio grows with height.
    public float EllipseRatio;
    public float BottomEllipseRatio;

    // Convex, counter-clockwise, starting at the middle of the bottom.
    public void FillPolygon(Vector2[] poly)
    {
        var n = 0;
        for (var i = 0; i <= BottomSegments; i++)
        {
            poly[n++] = BottomPoint(1f, (float)i / BottomSegments);
        }
        poly[n++] = new Vector2(BodyHalfWidth, ShoulderY);
        poly[n++] = new Vector2(NeckHalfWidth, NeckY);
        poly[n++] = new Vector2(NeckHalfWidth, LipY);
        poly[n++] = new Vector2(-NeckHalfWidth, LipY);
        poly[n++] = new Vector2(-NeckHalfWidth, NeckY);
        poly[n++] = new Vector2(-BodyHalfWidth, ShoulderY);
        for (var i = BottomSegments; i > 0; i--)
        {
            poly[n++] = BottomPoint(-1f, (float)i / BottomSegments);
        }
    }

    // A point on the rounded bottom; t runs from the middle (0) to where it meets the wall (1).
    private Vector2 BottomPoint(float side, float t)
    {
        var a = t * Mathf.PI * 0.5f;
        var e = 2f / BottomPower;
        // cos(pi/2) is a tiny negative float, and a negative base with a fractional power is NaN
        var x = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(a)), e) * BodyHalfWidth;
        var y = BottomY + BottomCurveHeight * (1f - Mathf.Pow(Mathf.Max(0f, Mathf.Cos(a)), e));
        return new Vector2(side * x, y);
    }

    // Matches Radius() in Liquid.shader: rounded shoulder.
    public float RadiusAt(float y)
    {
        var t = Mathf.Clamp01((y - ShoulderY) / Mathf.Max(NeckY - ShoulderY, 1e-4f));
        return Mathf.Lerp(BodyHalfWidth, NeckHalfWidth, t * t * (3f - 2f * t));
    }

    // Half height of the surface ellipse at a local height.
    public float EllipseAt(float y)
    {
        var t = Mathf.Clamp01((y - BottomY) / Mathf.Max(LipY - BottomY, 1e-4f));
        return Mathf.Lerp(BottomEllipseRatio, EllipseRatio, t) * RadiusAt(y);
    }

    // The lip corner that ends up lowest when the tube tilts by an angle of sign tiltSign.
    public Vector2 Lip(int tiltSign) => new Vector2(-tiltSign * NeckHalfWidth, LipY);
}

// Liquid stays level in world space: a local point p sits at world height dot(p, up) relative to the tube pivot.
public static class LiquidMath
{
    private static readonly Vector2[] s_clip = new Vector2[LiquidShape.VertexCount * 2];

    public static Vector2 LocalUp(float angleDeg)
    {
        var r = angleDeg * Mathf.Deg2Rad;
        return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
    }

    public static Vector2 LocalRight(Vector2 up) => new Vector2(up.y, -up.x);

    public static float Area(Vector2[] poly)
    {
        return ClipArea(poly, Vector2.up, float.MaxValue);
    }

    // Area of the polygon below the world-horizontal line dot(p, up) = h.
    public static float ClipArea(Vector2[] poly, Vector2 up, float h)
    {
        var n = poly.Length;
        var m = 0;
        for (var i = 0; i < n; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % n];
            var da = Vector2.Dot(a, up) - h;
            var db = Vector2.Dot(b, up) - h;
            if (da <= 0f) s_clip[m++] = a;
            if ((da < 0f && db > 0f) || (da > 0f && db < 0f))
            {
                s_clip[m++] = Vector2.Lerp(a, b, da / (da - db));
            }
        }

        var area = 0f;
        for (var i = 0; i < m; i++)
        {
            var a = s_clip[i];
            var b = s_clip[(i + 1) % m];
            area += a.x * b.y - b.x * a.y;
        }
        return area * 0.5f;
    }

    // World height (relative to pivot) of a level surface that holds the given area.
    public static float HeightForArea(Vector2[] poly, Vector2 up, float area)
    {
        var lo = float.MaxValue;
        var hi = float.MinValue;
        for (var i = 0; i < poly.Length; i++)
        {
            var d = Vector2.Dot(poly[i], up);
            lo = Mathf.Min(lo, d);
            hi = Mathf.Max(hi, d);
        }

        if (area <= 0f) return lo;

        for (var it = 0; it < 24; it++)
        {
            var mid = (lo + hi) * 0.5f;
            if (ClipArea(poly, up, mid) < area) lo = mid;
            else hi = mid;
        }
        return (lo + hi) * 0.5f;
    }

    // Where the level line dot(p, up) = h crosses the polygon, measured along world-right.
    public static void Chord(Vector2[] poly, Vector2 up, float h, out float wMin, out float wMax)
    {
        var right = LocalRight(up);
        wMin = float.MaxValue;
        wMax = float.MinValue;
        var n = poly.Length;
        for (var i = 0; i < n; i++)
        {
            var a = poly[i];
            var b = poly[(i + 1) % n];
            var da = Vector2.Dot(a, up) - h;
            var db = Vector2.Dot(b, up) - h;
            if ((da <= 0f && db >= 0f) || (da >= 0f && db <= 0f))
            {
                var t = Mathf.Abs(da - db) < 1e-6f ? 0.5f : da / (da - db);
                var w = Vector2.Dot(Vector2.Lerp(a, b, t), right);
                wMin = Mathf.Min(wMin, w);
                wMax = Mathf.Max(wMax, w);
            }
        }

        if (wMin > wMax)
        {
            wMin = wMax = 0f;
        }
    }

    // Smallest tilt (signed by tiltSign) at which a level surface holding `area` reaches the lip.
    public static float LipAngle(Vector2[] poly, LiquidShape shape, float area, int tiltSign, float maxAngle)
    {
        var lip = shape.Lip(tiltSign);

        float Gap(float deg)
        {
            var up = LocalUp(deg * tiltSign);
            return Vector2.Dot(lip, up) - HeightForArea(poly, up, area);
        }

        if (Gap(0f) <= 0f) return 0f;

        // coarse scan for the first sign change, then bisect inside that step
        const float step = 5f;
        var prev = 0f;
        var found = -1f;
        for (var deg = step; deg <= maxAngle; deg += step)
        {
            if (Gap(deg) <= 0f)
            {
                found = deg;
                break;
            }
            prev = deg;
        }

        if (found < 0f) return maxAngle * tiltSign;

        var lo = prev;
        var hi = found;
        for (var it = 0; it < 14; it++)
        {
            var mid = (lo + hi) * 0.5f;
            if (Gap(mid) > 0f) lo = mid;
            else hi = mid;
        }
        return hi * tiltSign;
    }
}
