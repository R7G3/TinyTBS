using Gum.GueDeriving;
using TinyTBS.Game.Maps.Models;

namespace TinyTBS.Game.Presentation.Match.Controls;

internal sealed class ShopOfferRowIcon(ContentId unitTypeId, SpriteRuntime baseSprite, SpriteRuntime maskSprite)
{
    public ContentId UnitTypeId { get; } = unitTypeId;

    public SpriteRuntime BaseSprite { get; } = baseSprite;

    public SpriteRuntime MaskSprite { get; } = maskSprite;
}
