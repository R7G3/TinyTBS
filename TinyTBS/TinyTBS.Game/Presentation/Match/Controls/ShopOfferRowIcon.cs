using Gum.GueDeriving;
using TinyTBS.Game.Match;

namespace TinyTBS.Game.Presentation.Match.Controls;

internal sealed class ShopOfferRowIcon(UnitKind unitKind, SpriteRuntime baseSprite, SpriteRuntime maskSprite)
{
    public UnitKind UnitKind { get; } = unitKind;

    public SpriteRuntime BaseSprite { get; } = baseSprite;

    public SpriteRuntime MaskSprite { get; } = maskSprite;
}
