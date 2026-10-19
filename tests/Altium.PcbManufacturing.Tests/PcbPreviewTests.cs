using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views;
using Altium.PcbManufacturing.Views.Graphics;
using Shape = System.Windows.Shapes.Path;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The vector board preview: the generated geometry, then the real top and side views laid out on an STA thread
/// (not shown, so rendering itself is not covered). Decorative coordinates are deliberately not checked.
/// </summary>
public sealed class PcbPreviewTests
{
    // Generated geometry

    [Theory]
    [InlineData(100, 80)]
    [InlineData(50, 200)]
    [InlineData(12, 9)]
    public void Artwork_EveryLayerIsFiniteAndInsideTheBoard(double width, double height)
    {
        PcbArtwork artwork = PcbArtworkBuilder.Build(width, height);
        Rect board = new(0, 0, width, height);

        Assert.Equal(width, artwork.Outline.Bounds.Width, precision: 3);
        Assert.Equal(height, artwork.Outline.Bounds.Height, precision: 3);
        foreach (Geometry layer in new[] { artwork.Traces, artwork.Pads, artwork.Silkscreen, artwork.MountingHoles })
        {
            Rect bounds = layer.Bounds;
            Assert.True(
                bounds.IsEmpty || (double.IsFinite(bounds.Width) && double.IsFinite(bounds.Height) && Inside(board, bounds)),
                $"A layer {bounds} leaves the {width} × {height} board.");
        }
    }

    [Fact]
    public void Artwork_ComponentsAreScaledUniformlyAndCentred()
    {
        PcbArtwork artwork = PcbArtworkBuilder.Build(120, 60);

        Matrix placement = artwork.Pads.Transform.Value;
        Assert.Equal(placement.M11, placement.M22, precision: 9);
        Rect designArea = new(0, 0, PcbArtworkBuilder.DesignSize.Width, PcbArtworkBuilder.DesignSize.Height);
        designArea.Transform(placement);
        Assert.Equal(designArea.Left, 120 - designArea.Right, precision: 9);
        Assert.Equal(designArea.Top, 60 - designArea.Bottom, precision: 9);
    }

    [Fact]
    public void Artwork_InvalidSize_Throws() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => PcbArtworkBuilder.Build(0, 80));

    // Top view

    [Theory]
    [InlineData(600, 150)]
    [InlineData(120, 400)]
    public void TopView_KeepsTheAspectRatioAndFitsCentredInAnyArea(double width, double height) => Sta.Run(() =>
    {
        PcbPreviewView view = new() { DataContext = CreateQuote(TestCatalogs.Create()).Quote };

        Rect board = LayOutBoard(view, width, height);

        // The test catalog's board is 120 × 60 mm.
        Assert.Equal(2.0, board.Width / board.Height, precision: 3);
        Assert.True(Math.Abs(board.Width - width) < 0.5 || Math.Abs(board.Height - height) < 0.5, $"Board does not fill the area: {board}.");
        Assert.Equal(board.Left, width - board.Right, precision: 1);
        Assert.Equal(board.Top, height - board.Bottom, precision: 1);
    });

    [Fact]
    public void TopView_IsFilledWithTheSelectedSolderMaskColour() => Sta.Run(() =>
    {
        PcbCatalog catalog = TestCatalogs.Create();
        (QuoteViewModel quote, ManufacturingPreferences preferences) = CreateQuote(catalog);
        PcbPreviewView view = new() { DataContext = quote };
        LayOutBoard(view, 300, 200);
        var outline = (Shape)view.FindName("Outline");

        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors[1];

        Assert.Equal(Color.FromRgb(0x00, 0x00, 0x00), ((SolidColorBrush)outline.Fill).Color);
    });

    // Side view, with the shipped catalog's materials, colours and thicknesses

    [Fact]
    public void SideView_CoreFollowsTheMaterialAndOuterLayersFollowTheSolderMask() => Sta.Run(() =>
    {
        (PcbSideView view, ManufacturingPreferences preferences, PcbCatalog catalog) = CreateSideView();
        LayOut(view, 400, 40);
        var core = (Border)view.FindName("Core");
        var topMask = (Border)((Grid)view.FindName("Profile")).Children[0];
        Color fr4 = BrushColor(core.Background);

        preferences.SelectedMaterial = catalog.Materials.Single(m => m.Id == "rogers-4350b");
        preferences.SelectedSolderMaskColor = catalog.SolderMaskColors.Single(c => c.Id == "blue");

        Assert.NotEqual(fr4, BrushColor(core.Background));
        Assert.Equal(Color.FromRgb(0x1F, 0x4E, 0x9C), BrushColor(topMask.Background));
    });

    [Fact]
    public void SideView_ThickerBoardsAreDrawnProportionallyThickerAndAlwaysFit() => Sta.Run(() =>
    {
        (PcbSideView view, ManufacturingPreferences preferences, PcbCatalog catalog) = CreateSideView();
        List<(decimal Millimeters, double Height)> profiles = [];

        foreach (BoardThickness thickness in catalog.BoardThicknesses.OrderBy(t => t.Millimeters))
        {
            preferences.SelectedBoardThickness = thickness;
            LayOut(view, 400, 40);
            var profile = (FrameworkElement)view.FindName("Profile");
            Rect bounds = profile.TransformToAncestor(view).TransformBounds(new Rect(profile.RenderSize));

            Assert.True(bounds.Top >= -0.5 && bounds.Bottom <= view.ActualHeight + 0.5, $"{thickness.Id} profile is clipped: {bounds}.");
            profiles.Add((thickness.Millimeters, bounds.Height));
        }

        for (int i = 1; i < profiles.Count; i++)
        {
            Assert.Equal((double)(profiles[i].Millimeters / profiles[0].Millimeters), profiles[i].Height / profiles[0].Height, precision: 2);
        }
    });

    private static (QuoteViewModel Quote, ManufacturingPreferences Preferences) CreateQuote(PcbCatalog catalog)
    {
        ManufacturingPreferences preferences = new(catalog);
        return (new QuoteViewModel(catalog, preferences, new PcbPricingService(catalog), new WorkflowNavigationState(), new Localizer()), preferences);
    }

    private static (PcbSideView View, ManufacturingPreferences Preferences, PcbCatalog Catalog) CreateSideView()
    {
        PcbCatalog shipped = new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync().GetAwaiter().GetResult();
        (QuoteViewModel quote, ManufacturingPreferences preferences) = CreateQuote(shipped);
        return (new PcbSideView { DataContext = quote }, preferences, shipped);
    }

    private static void LayOut(FrameworkElement view, double width, double height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, view.DesiredSize.Height));
        view.UpdateLayout();
    }

    private static Rect LayOutBoard(PcbPreviewView view, double width, double height)
    {
        view.Measure(new Size(width, height));
        view.Arrange(new Rect(0, 0, width, height));
        view.UpdateLayout();
        var board = (FrameworkElement)view.FindName("Board");
        return board.TransformToAncestor(view).TransformBounds(new Rect(board.RenderSize));
    }

    // Ellipse bounds come from a Bézier approximation, so containment allows a thousandth of a millimetre.
    private static bool Inside(Rect board, Rect bounds) =>
        bounds.Left >= board.Left - 1e-3 && bounds.Top >= board.Top - 1e-3
        && bounds.Right <= board.Right + 1e-3 && bounds.Bottom <= board.Bottom + 1e-3;

    private static Color BrushColor(Brush brush) => ((SolidColorBrush)brush).Color;
}
