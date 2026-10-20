using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Altium.PcbManufacturing.Localization;
using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.Services;
using Altium.PcbManufacturing.ViewModels;
using Altium.PcbManufacturing.Views;
using Shape = System.Windows.Shapes.Path;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The real top and side previews follow material, solder mask, and thickness changes.
/// Views are laid out on an STA thread; visual appearance is checked manually.
/// </summary>
public sealed class PcbPreviewTests
{
    [Fact]
    public void TopView_IsFilledWithTheSelectedSolderMaskColour() => Sta.Run(() =>
    {
        PcbCatalog catalog = TestCatalogs.Create();
        (QuoteViewModel quote, ManufacturingOrderDraft draft) = CreateQuote(catalog);
        PcbPreviewView view = new() { DataContext = quote };
        LayOutBoard(view, 300, 200);
        var outline = (Shape)view.FindName("Outline");

        draft.SelectedSolderMaskColor = catalog.SolderMaskColors[1];

        Assert.Equal(Color.FromRgb(0x00, 0x00, 0x00), ((SolidColorBrush)outline.Fill).Color);
    });

    // Side view, with the shipped catalog's materials, colours and thicknesses

    [Fact]
    public void SideView_CoreFollowsTheMaterialAndOuterLayersFollowTheSolderMask() => Sta.Run(() =>
    {
        (PcbSideView view, ManufacturingOrderDraft draft, PcbCatalog catalog) = CreateSideView();
        LayOut(view, 400, 40);
        var core = (Border)view.FindName("Core");
        var topMask = (Border)((Grid)view.FindName("Profile")).Children[0];
        Color fr4 = BrushColor(core.Background);

        draft.SelectedMaterial = catalog.Materials.Single(m => m.Id == "rogers-4350b");
        draft.SelectedSolderMaskColor = catalog.SolderMaskColors.Single(c => c.Id == "blue");

        Assert.NotEqual(fr4, BrushColor(core.Background));
        Assert.Equal(Color.FromRgb(0x1F, 0x4E, 0x9C), BrushColor(topMask.Background));
    });

    [Fact]
    public void SideView_ThickerBoardsAreDrawnProportionallyThickerAndAlwaysFit() => Sta.Run(() =>
    {
        (PcbSideView view, ManufacturingOrderDraft draft, PcbCatalog catalog) = CreateSideView();
        List<(decimal Millimeters, double Height)> profiles = [];

        foreach (BoardThickness thickness in catalog.BoardThicknesses.OrderBy(t => t.Millimeters))
        {
            draft.SelectedBoardThickness = thickness;
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

    private static (QuoteViewModel Quote, ManufacturingOrderDraft Draft) CreateQuote(PcbCatalog catalog)
    {
        ManufacturingOrderDraft draft = new(catalog, new PcbPricingService(catalog));
        return (new QuoteViewModel(catalog, draft, new Localizer()), draft);
    }

    private static (PcbSideView View, ManufacturingOrderDraft Draft, PcbCatalog Catalog) CreateSideView()
    {
        PcbCatalog shipped = new PcbCatalogLoader(PcbCatalogLoader.DefaultCatalogPath).LoadAsync().GetAwaiter().GetResult();
        (QuoteViewModel quote, ManufacturingOrderDraft draft) = CreateQuote(shipped);
        return (new PcbSideView { DataContext = quote }, draft, shipped);
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

    private static Color BrushColor(Brush brush) => ((SolidColorBrush)brush).Color;
}
