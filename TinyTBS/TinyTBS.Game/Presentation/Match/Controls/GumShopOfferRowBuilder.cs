using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using TinyTBS.Engine.GumLayout;
using TinyTBS.Game.Match;
using TinyTBS.Game.ViewModels;

namespace TinyTBS.Game.Presentation.Match.Controls;

    /// <summary>One shop offer row: full-width button, icon, name/stats, price.</summary>
    internal static class GumShopOfferRowBuilder
    {
        public static Panel AddOfferRow(
            Panel offersPanel,
            GameplayShopOfferViewModel offer,
            Action<int> onBuyOffer,
            ICollection<Button> offerButtons,
            ICollection<ShopOfferRowIcon> offerIcons)
        {
            const float iconSize = GumTeamIconSlot.DefaultIconSize;
            const float padding = 8f;

            var row = new Panel();
            row.Visual.HasEvents = false;
            row.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
            row.Visual.MinHeight = iconSize + padding * 2;
            GumUiLayout.FillParentWidth(row);
            offersPanel.AddChild(row);

            var offerButton = new Button { Text = string.Empty };
            offerButton.Dock(Dock.Fill);
            offerButton.IsEnabled = offer.CanAfford;
            var offerIndex = offer.OfferIndex;
            offerButton.Click += (_, _) => onBuyOffer(offerIndex);
            row.AddChild(offerButton);
            offerButtons.Add(offerButton);

            var shell = new Panel();
            shell.Visual.HasEvents = false;
            shell.Visual.X = padding;
            shell.Visual.Y = 0;
            shell.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            shell.Visual.Width = -(padding * 2);
            shell.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
            shell.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
            shell.Visual.StackSpacing = 0;
            row.AddChild(shell);

            GumUiLayout.AddVerticalSpacer(shell, padding);

            var content = new Panel();
            content.Visual.HasEvents = false;
            content.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            content.Visual.Width = 0;
            content.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
            content.Visual.ChildrenLayout = ChildrenLayout.LeftToRightStack;
            content.Visual.StackSpacing = 12;
            shell.AddChild(content);

            var iconColumn = new Panel();
            iconColumn.Visual.HasEvents = false;
            iconColumn.Visual.Width = iconSize;
            iconColumn.Visual.WidthUnits = DimensionUnitType.Absolute;
            iconColumn.Visual.HeightUnits = DimensionUnitType.RelativeToParent;
            iconColumn.Visual.Height = 0;
            content.AddChild(iconColumn);

            var iconSlot = GumTeamIconSlot.AddCenteredInColumn(iconColumn, withTeamMask: true, iconSize);
            offerIcons.Add(new ShopOfferRowIcon(offer.UnitTypeId, iconSlot.BaseSprite, iconSlot.MaskSprite!));

            var textColumn = new Panel();
            textColumn.Visual.HasEvents = false;
            textColumn.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            textColumn.Visual.Width = -(iconSize + 12f);
            textColumn.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
            textColumn.Visual.ChildrenLayout = ChildrenLayout.TopToBottomStack;
            textColumn.Visual.StackSpacing = 2;
            content.AddChild(textColumn);

            var titleRow = new Panel();
            titleRow.Visual.HasEvents = false;
            titleRow.Visual.WidthUnits = DimensionUnitType.RelativeToParent;
            titleRow.Visual.Width = 0;
            titleRow.Visual.HeightUnits = DimensionUnitType.RelativeToChildren;
            textColumn.AddChild(titleRow);

            var nameLabel = new Label { Text = offer.Name };
            nameLabel.Visual.HasEvents = false;
            titleRow.AddChild(nameLabel);

            var costLine = offer.CanAfford
                ? $"{offer.Cost}g"
                : $"{offer.Cost}g (not enough)";
            var priceLabel = new Label { Text = costLine };
            priceLabel.Visual.HasEvents = false;
            priceLabel.Anchor(Anchor.Right);
            priceLabel.X = 0;
            titleRow.AddChild(priceLabel);

            var statsLabel = new Label { Text = offer.StatsText };
            statsLabel.Visual.HasEvents = false;
            GumUiLayout.FillParentWidth(statsLabel);
            textColumn.AddChild(statsLabel);

            GumUiLayout.AddVerticalSpacer(shell, padding);
            return row;
        }

    public static bool OffersEqual(
        IReadOnlyList<GameplayShopOfferViewModel> left,
        IReadOnlyList<GameplayShopOfferViewModel> right)
    {
        if (ReferenceEquals(left, right))
            return true;
        if (left.Count != right.Count)
            return false;

        for (var index = 0; index < left.Count; index++)
        {
            if (left[index].OfferIndex != right[index].OfferIndex
                || left[index].UnitTypeId != right[index].UnitTypeId
                || left[index].Cost != right[index].Cost
                || left[index].CanAfford != right[index].CanAfford
                || left[index].Name != right[index].Name
                || left[index].StatsText != right[index].StatsText)
            {
                return false;
            }
        }

        return true;
    }
}
