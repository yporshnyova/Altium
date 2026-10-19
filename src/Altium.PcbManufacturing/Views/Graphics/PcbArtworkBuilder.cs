using System.Windows;
using System.Windows.Media;

namespace Altium.PcbManufacturing.Views.Graphics;

/// <summary>
/// The layers of the illustrative top-view drawing of a board, in millimetres with the origin at the board's top-left
/// corner. Every geometry is frozen and filled (strokes are already widened), so the view only assigns brushes.
/// </summary>
/// <param name="Outline">Board outline with rounded corners; filled with the solder mask colour.</param>
/// <param name="Traces">Copper traces under the mask.</param>
/// <param name="Pads">Exposed copper: component pads, the connector header and vias.</param>
/// <param name="Silkscreen">Printed component outlines and the pin-1 marker.</param>
/// <param name="MountingHoles">Drilled holes near the corners; empty when the board is too small for them.</param>
/// <param name="ArtworkScale">Scale applied to the component artwork so it fits the board without distortion.</param>

public sealed record PcbArtwork(
    Geometry Outline,
    Geometry Traces,
    Geometry Pads,
    Geometry Silkscreen,
    Geometry MountingHoles,
    double ArtworkScale);

/// <summary>
/// Builds the top-view drawing of a board from its width and height.
/// </summary>

public static class PcbArtworkBuilder
{
    public const double CornerRadiusMm = 3;
    public const double MountingHoleInsetMm = 5;
    public const double MountingHoleRadiusMm = 2.2;

    /// <summary>Size of the design grid the component artwork is drawn on.</summary>
    public static readonly Size DesignSize = new(100, 80);

    private const double traceWidth = 1.2;
    private const double silkscreenWidth = 0.5;
        
    private static readonly Point[][] tracePaths =
    [
        [new(56, 26), new(59, 26), new(61, 19.3), new(63, 19.3)],
        [new(56, 30), new(59, 30), new(61, 27.3), new(63, 27.3)],
        [new(56, 34), new(59, 34), new(60.3, 35.3), new(63, 35.3)],
        [new(68.1, 27.3), new(74, 27.3), new(74, 44), new(76, 46)],
        [new(40, 50), new(40, 58), new(48, 66), new(68, 66)],
        [new(24, 34), new(14, 34), new(14, 62), new(20, 68), new(34.6, 68)],
        [new(40, 18), new(40, 12), new(86, 12), new(90, 16), new(90, 30.6)],
    ];

    public static PcbArtwork Build(double widthMm, double heightMm)
    {
        if (!double.IsFinite(widthMm) || widthMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(widthMm), widthMm, "The board width must be a positive number.");
        }

        if (!double.IsFinite(heightMm) || heightMm <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(heightMm), heightMm, "The board height must be a positive number.");
        }

        double scale = Math.Min(widthMm / DesignSize.Width, heightMm / DesignSize.Height);
        var placement = new MatrixTransform(
            scale, 0, 0, scale,
            (widthMm - (DesignSize.Width * scale)) / 2,
            (heightMm - (DesignSize.Height * scale)) / 2);
        placement.Freeze();

        var pads = new GeometryGroup { FillRule = FillRule.Nonzero };
        AddQuadFlatPackPads(pads, center: new Point(40, 34), bodySize: 24, pinsPerSide: 5, pitch: 4, padLength: 4, padWidth: 1.4);
        AddHeaderPads(pads, firstPin: new Point(70, 48), columns: 2, rows: 5, pitch: 6, padRadius: 1.6);
        foreach (double y in new[] { 18d, 26d, 34d })
        {
            AddTwoPadPassive(pads, new Point(62, y), padSize: new Size(2.2, 2.6), spacing: 5);
        }

        AddCircle(pads, new Point(36, 68), 1.4);
        AddCircle(pads, new Point(90, 32), 1.4);

        var silkscreenOutlines = new GeometryGroup();
        silkscreenOutlines.Children.Add(new RectangleGeometry(new Rect(28, 22, 24, 24)));
        silkscreenOutlines.Children.Add(new RectangleGeometry(new Rect(66, 44, 14, 32)));
        Geometry silkscreen = Combine(
            Widen(silkscreenOutlines, silkscreenWidth),
            new EllipseGeometry(new Point(30.6, 24.6), 0.8, 0.8));

        return new PcbArtwork(
            Outline: Frozen(CreateOutline(widthMm, heightMm)),
            Traces: Place(Widen(CreateTraces(), traceWidth), placement),
            Pads: Place(pads, placement),
            Silkscreen: Place(silkscreen, placement),
            MountingHoles: Frozen(CreateMountingHoles(widthMm, heightMm)),
            ArtworkScale: scale);
    }

    private static RectangleGeometry CreateOutline(double width, double height)
    {
        double radius = Math.Min(CornerRadiusMm, Math.Min(width, height) / 4);
        return new RectangleGeometry(new Rect(0, 0, width, height), radius, radius);
    }

    // One hole per corner, only when the board leaves room for a hole and its clearance on every side.
    private static GeometryGroup CreateMountingHoles(double width, double height)
    {
        var holes = new GeometryGroup();
        if (Math.Min(width, height) < 4 * MountingHoleInsetMm)
        {
            return holes;
        }

        foreach (double x in new[] { MountingHoleInsetMm, width - MountingHoleInsetMm })
        {
            foreach (double y in new[] { MountingHoleInsetMm, height - MountingHoleInsetMm })
            {
                AddCircle(holes, new Point(x, y), MountingHoleRadiusMm);
            }
        }

        return holes;
    }

    private static StreamGeometry CreateTraces()
    {
        var traces = new StreamGeometry();
        using (StreamGeometryContext context = traces.Open())
        {
            foreach (Point[] path in tracePaths)
            {
                context.BeginFigure(path[0], isFilled: false, isClosed: false);
                context.PolyLineTo(path[1..], isStroked: true, isSmoothJoin: true);
            }
        }

        return traces;
    }

    // Pads around a square body: pinsPerSide pads centred on each side, pointing outwards.
    private static void AddQuadFlatPackPads(
        GeometryGroup pads, Point center, double bodySize, int pinsPerSide, double pitch, double padLength, double padWidth)
    {
        double half = bodySize / 2;
        for (int i = 0; i < pinsPerSide; i++)
        {
            double offset = (i - ((pinsPerSide - 1) / 2.0)) * pitch;
            pads.Children.Add(new RectangleGeometry(new Rect(center.X - half - padLength, center.Y + offset - (padWidth / 2), padLength, padWidth)));
            pads.Children.Add(new RectangleGeometry(new Rect(center.X + half, center.Y + offset - (padWidth / 2), padLength, padWidth)));
            pads.Children.Add(new RectangleGeometry(new Rect(center.X + offset - (padWidth / 2), center.Y - half - padLength, padWidth, padLength)));
            pads.Children.Add(new RectangleGeometry(new Rect(center.X + offset - (padWidth / 2), center.Y + half, padWidth, padLength)));
        }
    }

    private static void AddHeaderPads(GeometryGroup pads, Point firstPin, int columns, int rows, double pitch, double padRadius)
    {
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                AddCircle(pads, new Point(firstPin.X + (column * pitch), firstPin.Y + (row * pitch)), padRadius);
            }
        }
    }

    private static void AddTwoPadPassive(GeometryGroup pads, Point firstPad, Size padSize, double spacing)
    {
        pads.Children.Add(new RectangleGeometry(new Rect(firstPad, padSize)));
        pads.Children.Add(new RectangleGeometry(new Rect(new Point(firstPad.X + spacing, firstPad.Y), padSize)));
    }

    private static void AddCircle(GeometryGroup group, Point center, double radius) =>
        group.Children.Add(new EllipseGeometry(center, radius, radius));

    // Converts strokes into filled outlines so the drawing does not depend on the view's pen settings.
    private static PathGeometry Widen(Geometry geometry, double width)
    {
        var pen = new Pen(Brushes.Black, width)
        {
            StartLineCap = PenLineCap.Round,
            EndLineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round,
        };
        return geometry.GetWidenedPathGeometry(pen);
    }

    private static GeometryGroup Combine(params Geometry[] geometries)
    {
        var group = new GeometryGroup { FillRule = FillRule.Nonzero };
        foreach (Geometry geometry in geometries)
        {
            group.Children.Add(geometry);
        }

        return group;
    }

    private static Geometry Place(Geometry geometry, Transform placement)
    {
        geometry.Transform = placement;
        return Frozen(geometry);
    }

    private static Geometry Frozen(Geometry geometry)
    {
        geometry.Freeze();
        return geometry;
    }
}
