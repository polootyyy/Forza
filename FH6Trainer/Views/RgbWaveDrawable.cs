using Microsoft.Maui.Graphics;

namespace Polootyyy.Views;

public class RgbWaveDrawable : IDrawable
{
    private float _time;
    private const float WaveSpeed = 0.02f;
    private const float WaveThickness = 3f;
    private const float CornerRadius = 16f;

    public float Time { get => _time; set => _time = value; }

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        float w = dirtyRect.Width;
        float h = dirtyRect.Height;
        float r = CornerRadius;

        // Draw the RGB wave border
        canvas.StrokeSize = WaveThickness;
        canvas.StrokeLineCap = LineCap.Round;
        canvas.StrokeLineJoin = LineJoin.Round;

        // Create gradient path along the border
        int segments = 120;
        float perimeter = 2 * (w + h) - 8 * r + (float)(2 * Math.PI * r);

        for (int i = 0; i < segments; i++)
        {
            float t = (float)i / segments;
            float hue = (t + _time * 0.3f) % 1.0f;
            var color = HsvToRgb(hue, 1f, 1f);
            canvas.StrokeColor = color;

            float dist = t * perimeter;
            var p1 = PointOnBorder(dist, w, h, r);
            var p2 = PointOnBorder((t + 1f / segments) * perimeter, w, h, r);

            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y);
        }

        // Draw glow effect - wider, more transparent
        canvas.StrokeSize = WaveThickness * 3;
        for (int i = 0; i < segments; i += 3)
        {
            float t = (float)i / segments;
            float hue = (t + _time * 0.3f) % 1.0f;
            var color = HsvToRgb(hue, 1f, 0.3f);
            canvas.StrokeColor = color;

            float dist = t * perimeter;
            var p1 = PointOnBorder(dist, w, h, r);
            var p2 = PointOnBorder((t + 3f / segments) * perimeter, w, h, r);

            canvas.DrawLine(p1.X, p1.Y, p2.X, p2.Y);
        }
    }

    private static PointF PointOnBorder(float distance, float w, float h, float r)
    {
        float topLen = w - 2 * r;
        float sideLen = h - 2 * r;
        float cornerLen = (float)(Math.PI * r / 2);
        float totalTop = topLen;
        float totalRight = sideLen;
        float totalBottom = topLen;
        float totalLeft = sideLen;

        distance = distance % (2 * totalTop + 2 * totalRight + 4 * cornerLen);

        // Top edge (left to right)
        if (distance < totalTop)
            return new PointF(r + distance, 0);

        distance -= totalTop;
        // Top-right corner
        if (distance < cornerLen)
        {
            float angle = (float)(Math.PI * 1.5) + (distance / cornerLen) * (float)(Math.PI / 2);
            return new PointF(w - r + r * MathF.Cos(angle), r + r * MathF.Sin(angle));
        }

        distance -= cornerLen;
        // Right edge (top to bottom)
        if (distance < totalRight)
            return new PointF(w, r + distance);

        distance -= totalRight;
        // Bottom-right corner
        if (distance < cornerLen)
        {
            float angle = 0 + (distance / cornerLen) * (float)(Math.PI / 2);
            return new PointF(w - r + r * MathF.Cos(angle), h - r + r * MathF.Sin(angle));
        }

        distance -= cornerLen;
        // Bottom edge (right to left)
        if (distance < totalBottom)
            return new PointF(w - r - distance, h);

        distance -= totalBottom;
        // Bottom-left corner
        if (distance < cornerLen)
        {
            float angle = (float)(Math.PI * 0.5) + (distance / cornerLen) * (float)(Math.PI / 2);
            return new PointF(r + r * MathF.Cos(angle), h - r + r * MathF.Sin(angle));
        }

        distance -= cornerLen;
        // Left edge (bottom to top)
        if (distance < totalLeft)
            return new PointF(0, h - r - distance);

        distance -= totalLeft;
        // Top-left corner
        {
            float angle = (float)Math.PI + (distance / cornerLen) * (float)(Math.PI / 2);
            return new PointF(r + r * MathF.Cos(angle), r + r * MathF.Sin(angle));
        }
    }

    private static Color HsvToRgb(float h, float s, float v)
    {
        int hi = (int)(h * 6) % 6;
        float f = h * 6 - (int)(h * 6);
        float p = v * (1 - s);
        float q = v * (1 - f * s);
        float t = v * (1 - (1 - f) * s);

        return hi switch
        {
            0 => new Color(v, t, p),
            1 => new Color(q, v, p),
            2 => new Color(p, v, t),
            3 => new Color(p, q, v),
            4 => new Color(t, p, v),
            _ => new Color(v, p, q),
        };
    }
}
