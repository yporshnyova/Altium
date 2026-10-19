using Altium.PcbManufacturing.Localization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Altium.PcbManufacturing.ViewModels;

public sealed class QuoteParameter : ObservableObject
{
    private readonly Localizer localizer;
    private readonly Func<string> value;
    private readonly Func<string?>? swatchHexColor;

    public QuoteParameter(Localizer localizer, string labelKey, Func<string> value, Func<string?>? swatchHexColor = null)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrWhiteSpace(labelKey);
        ArgumentNullException.ThrowIfNull(value);

        this.localizer = localizer;
        this.value = value;
        this.swatchHexColor = swatchHexColor;
        LabelKey = labelKey;
    }

    public string LabelKey { get; }

    public string Label => localizer[LabelKey];

    public string Value => value();

    public string? SwatchHexColor => swatchHexColor?.Invoke();

    public bool HasSwatch => swatchHexColor is not null;

    public void Refresh()
    {
        OnPropertyChanged(nameof(Label));
        OnPropertyChanged(nameof(Value));
        OnPropertyChanged(nameof(SwatchHexColor));
    }
}

/// <summary>
/// A titled group of <see cref="QuoteParameter"/> rows, e.g. Dimensions or Layers.
/// </summary>
public sealed class QuoteParameterGroup : ObservableObject
{
    private readonly Localizer localizer;

    public QuoteParameterGroup(Localizer localizer, string titleKey, IReadOnlyList<QuoteParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(localizer);
        ArgumentException.ThrowIfNullOrWhiteSpace(titleKey);
        ArgumentNullException.ThrowIfNull(parameters);

        this.localizer = localizer;
        TitleKey = titleKey;
        Parameters = parameters;
    }

    public string TitleKey { get; }

    public string Title => localizer[TitleKey];

    public IReadOnlyList<QuoteParameter> Parameters { get; }

    public void Refresh()
    {
        OnPropertyChanged(nameof(Title));
        foreach (QuoteParameter parameter in Parameters)
        {
            parameter.Refresh();
        }
    }
}
