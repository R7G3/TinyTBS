using Gum;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Loading;

/// <summary>Presentation: title, stage label, and a simple progress bar for match load.</summary>
public sealed class LoadingView
{
    private static readonly Color TrackColor = new(40, 48, 64);
    private static readonly Color FillColor = new(90, 160, 220);

    private Panel? _rootPanel;
    private Label? _stageLabel;
    private Label? _percentLabel;
    private RectangleRuntime? _fillBar;

    public void Build(LoadingViewModel viewModel)
    {
        Clear();

        _rootPanel = new Panel();
        _rootPanel.Dock(Dock.Fill);
        _rootPanel.AddToRoot();

        var bodyPanel = new Panel();
        bodyPanel.Dock(Dock.Fill);
        _rootPanel.AddChild(bodyPanel);

        var contentPanel = GumUiLayout.CreateVerticalStackPanel(spacing: 14f, widthPercent: 90f);
        GumUiLayout.SetBoundedWidth(contentPanel, maxPixels: 520f, parentPercent: 90f);
        GumUiLayout.CenterInParent(contentPanel, xPercent: 50f, yPercent: 48f);
        bodyPanel.AddChild(contentPanel);

        var title = new Label { Text = viewModel.Title };
        GumUiLayout.FillParentWidth(title);
        contentPanel.AddChild(title);

        _stageLabel = new Label { Text = viewModel.StageLabel };
        GumUiLayout.FillParentWidth(_stageLabel);
        contentPanel.AddChild(_stageLabel);

        var track = new Panel();
        GumUiLayout.FillParentWidth(track);
        GumUiLayout.SetAbsoluteHeight(track, pixels: 18f);
        GumUiLayout.AddSolidBackground(track, TrackColor);
        contentPanel.AddChild(track);

        _fillBar = new RectangleRuntime
        {
            Height = 0,
            HeightUnits = Gum.DataTypes.DimensionUnitType.RelativeToParent,
            Width = 0,
            WidthUnits = Gum.DataTypes.DimensionUnitType.PercentageOfParent,
            X = 0,
            Y = 0,
            FillColor = FillColor,
            IsFilled = true,
        };
        track.AddChild(_fillBar);

        _percentLabel = new Label { Text = viewModel.ProgressPercentText };
        GumUiLayout.FillParentWidth(_percentLabel);
        contentPanel.AddChild(_percentLabel);

        Sync(viewModel);
    }

    public void Sync(LoadingViewModel viewModel)
    {
        if (_stageLabel is not null)
            _stageLabel.Text = viewModel.StageLabel;
        if (_percentLabel is not null)
            _percentLabel.Text = viewModel.ProgressPercentText;
        if (_fillBar is not null)
            _fillBar.Width = Math.Clamp(viewModel.ProgressFraction, 0f, 1f) * 100f;
    }

    public void Clear()
    {
        GumService.Default.Root.Children.Clear();
        _rootPanel = null;
        _stageLabel = null;
        _percentLabel = null;
        _fillBar = null;
    }
}
