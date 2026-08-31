using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;
using MonoGame.Framework.Content.Pipeline.Builder;

namespace TinyTBS.Content;

/// <summary>
/// C# Content Builder (MonoGame 3.8.5+): describes bundled assets with include rules.
/// Uses <see cref="RegexRule"/> because <see cref="WildcardRule"/> is slash-sensitive
/// and does not match Windows <c>\</c> paths reliably.
/// </summary>
public sealed class TinyTbsContentBuilder : ContentBuilder
{
    public override IContentCollection GetContentCollection()
    {
        var content = new ContentCollection();

        // Pixel-art friendly: keep Color (no DXT) so team-mask alphas stay clean.
        var textureProcessor = new TextureProcessor
        {
            ColorKeyEnabled = false,
            PremultiplyAlpha = true,
            GenerateMipmaps = false,
            TextureFormat = TextureProcessorOutputFormat.Color,
        };

        // Accept both / and \ separators under Images/ and Sounds/.
        content.Include<RegexRule>(
            @"(?i)^Images[/\\].+\.png$",
            new TextureImporter(),
            textureProcessor);

        content.Include<RegexRule>(
            @"(?i)^Sounds[/\\].+\.ogg$",
            new OggImporter(),
            new SoundEffectProcessor());

        content.Exclude<RegexRule>(@"(?i)(^|[/\\])\.gitkeep$");

        return content;
    }
}
