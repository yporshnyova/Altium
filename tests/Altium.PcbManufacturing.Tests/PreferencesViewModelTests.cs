using Altium.PcbManufacturing.Models;
using Altium.PcbManufacturing.ViewModels;

namespace Altium.PcbManufacturing.Tests;

/// <summary>
/// The Preferences step: the raw input and its validation (project name, postcode, quantity, notes), how valid input
/// reaches the draft, the side-effect-free validity the wizard relies on, and Reset to Defaults. Where Save leads is
/// covered by <see cref="OrderWizardTests"/>.
/// </summary>
public sealed class PreferencesViewModelTests
{
    private const string quantityWholeNumberMessage = "Enter a whole number of boards, 1 or more.";

    private readonly TestWizard wizard = new();

    private PreferencesViewModel ViewModel => wizard.Preferences;

    private ManufacturingOrderDraft Draft => wizard.Draft;

    private PcbCatalog Catalog => wizard.Catalog;

    [Fact]
    public void Initially_ShowsTheDraftWithoutErrors()
    {
        Assert.Same(Draft, ViewModel.Draft);
        Assert.Equal("PCB-001", ViewModel.ProjectNameInput);
        Assert.Equal(string.Empty, ViewModel.PostcodeInput);
        Assert.Equal("10", ViewModel.QuantityInput);
        Assert.Equal(string.Empty, ViewModel.NotesInput);
        // The empty postcode makes the input invalid, but it is not reported until edited or submitted.
        Assert.False(ViewModel.IsInputValid);
        Assert.False(ViewModel.HasErrors);
    }

    [Fact]
    public void ValidInput_IsWrittenToTheDraft()
    {
        ViewModel.ProjectNameInput = "Sensor board";
        ViewModel.PostcodeInput = "55-220";
        ViewModel.QuantityInput = "3";
        ViewModel.NotesInput = "Rush";

        Assert.Equal("Sensor board", Draft.ProjectName);
        Assert.Equal("55-220", Draft.Postcode);
        Assert.Equal(3, Draft.Quantity);
        Assert.Equal("Rush", Draft.Notes);
        Assert.Equal(447.00m, Draft.TotalPrice);
        Assert.True(ViewModel.IsInputValid);
    }

    // Validation

    [Theory]
    [InlineData("Płytka główna #3 (v2.1)", null)]
    [InlineData("", "Enter a project name.")]
    [InlineData("   ", "Enter a project name.")]
    public void ProjectName_IsRequiredAndKeptAsTyped(string projectName, string? error)
    {
        ViewModel.ProjectNameInput = projectName;

        Assert.Equal(error is null ? [] : [error], Errors(nameof(PreferencesViewModel.ProjectNameInput)));
        Assert.Equal(projectName, Draft.ProjectName);
    }

    [Fact]
    public void ProjectName_IsLimitedTo50Characters()
    {
        // The TextBox blocks typing past the limit; a longer value can still be assigned in code.
        ViewModel.ProjectNameInput = new string('a', 50);
        Assert.False(ViewModel.HasProjectNameErrors);

        ViewModel.ProjectNameInput = new string('a', 51);
        Assert.Equal(["Use at most 50 characters."], Errors(nameof(PreferencesViewModel.ProjectNameInput)));
    }

    [Theory]
    [InlineData("55-220", null)]
    [InlineData("55220", "Use the NN-NNN format.")]
    [InlineData("55-220 ", "Use the NN-NNN format.")]
    [InlineData("", "Enter a postcode.")]
    public void Postcode_IsValidatedButAlwaysKept(string postcode, string? error)
    {
        ViewModel.PostcodeInput = "5";

        ViewModel.PostcodeInput = postcode;

        Assert.Equal(error is null ? [] : [error], Errors(nameof(PreferencesViewModel.PostcodeInput)));
        Assert.Equal(postcode, Draft.Postcode);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("007", 7)]
    [InlineData("2147483647", int.MaxValue)]
    public void Quantity_ValidWholeNumber_IsWrittenToTheDraft(string input, int expected)
    {
        ViewModel.QuantityInput = input;

        Assert.False(ViewModel.HasQuantityErrors);
        Assert.Equal(expected, Draft.Quantity);
    }

    // Typing is filtered to digits in the view, but bound or programmatic text is still validated here.
    [Theory]
    [InlineData("0", quantityWholeNumberMessage)]
    [InlineData("-5", quantityWholeNumberMessage)]
    [InlineData("2.5", quantityWholeNumberMessage)]
    [InlineData("abc", quantityWholeNumberMessage)]
    [InlineData("", "Enter the number of boards.")]
    [InlineData("2147483648", "The quantity is too large.")]
    public void Quantity_InvalidText_IsReportedAndKeptWhileTheDraftKeepsTheLastValidQuantity(string input, string error)
    {
        ViewModel.QuantityInput = "4";

        ViewModel.QuantityInput = input;

        Assert.Equal([error], Errors(nameof(PreferencesViewModel.QuantityInput)));
        Assert.Equal(input, ViewModel.QuantityInput);
        Assert.Equal(4, Draft.Quantity);
        Assert.Equal(596.00m, Draft.TotalPrice);
        Assert.False(ViewModel.IsInputValid);
    }

    [Fact]
    public void Quantity_CorrectedAfterInvalidText_IsValidAndWrittenAgain()
    {
        ViewModel.QuantityInput = "abc";

        ViewModel.QuantityInput = "3";

        Assert.False(ViewModel.HasErrors);
        Assert.Equal(3, Draft.Quantity);
    }

    [Fact]
    public void Notes_AreOptionalAndLimitedTo300Characters()
    {
        ViewModel.NotesInput = new string('n', 300);
        Assert.False(ViewModel.HasErrors);
        Assert.Equal("300 / 300", ViewModel.NotesCounterText);

        ViewModel.NotesInput = new string('n', 301);
        Assert.Equal(["Use at most 300 characters."], Errors(nameof(PreferencesViewModel.NotesInput)));
    }

    [Fact]
    public void IsInputValid_FollowsEveryFieldAndIsNotified()
    {
        List<string?> changed = [];
        ViewModel.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        ViewModel.PostcodeInput = "55-220";
        Assert.True(ViewModel.IsInputValid);
        Assert.Contains(nameof(PreferencesViewModel.IsInputValid), changed);

        ViewModel.QuantityInput = "abc";
        Assert.False(ViewModel.IsInputValid);

        ViewModel.QuantityInput = "5";
        ViewModel.ProjectNameInput = "";
        Assert.False(ViewModel.IsInputValid);
    }

    [Fact]
    public void Reset_Confirmed_RestoresDefaultsEvenOverInvalidTextAndClearsErrors()
    {
        ChangeEverything();
        ViewModel.PostcodeInput = "55-2";
        ViewModel.QuantityInput = "abc";

        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(wizard.Confirmation.Requests);
        Assert.True(Draft.MatchesDefaults());
        Assert.Equal("10", ViewModel.QuantityInput);
        Assert.Equal(string.Empty, ViewModel.PostcodeInput);
        Assert.False(ViewModel.HasErrors);
    }

    [Fact]
    public void Reset_Cancelled_KeepsEveryValue()
    {
        ChangeEverything();
        wizard.Confirmation.Answer = false;

        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(wizard.Confirmation.Requests);
        Assert.Equal("Sensor board", Draft.ProjectName);
        Assert.Same(Catalog.Materials[1], Draft.SelectedMaterial);
        Assert.Equal(25, Draft.Quantity);
        Assert.Equal("25", ViewModel.QuantityInput);
    }

    [Fact]
    public void Reset_WithOnlyInvalidQuantityText_AsksBecauseTheTextWouldBeLost()
    {
        ViewModel.QuantityInput = "abc";

        ViewModel.ResetToDefaultsCommand.Execute(null);

        Assert.Single(wizard.Confirmation.Requests);
        Assert.Equal("10", ViewModel.QuantityInput);
    }

    private void ChangeEverything()
    {
        ViewModel.ProjectNameInput = "Sensor board";
        ViewModel.PostcodeInput = "55-220";
        ViewModel.QuantityInput = "25";
        Draft.SelectedMaterial = Catalog.Materials[1];
        Draft.SelectedSolderMaskColor = Catalog.SolderMaskColors[1];
        Draft.SelectedBoardThickness = Catalog.BoardThicknesses[1];
        ViewModel.NotesInput = "Rush";
    }

    private IEnumerable<string?> Errors(string propertyName) =>
        ViewModel.GetErrors(propertyName).Select(result => result.ErrorMessage);
}
