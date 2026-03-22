using Assets.Scripts.Buildings;
using Assets.Scripts.Units;

namespace Assets.Scripts.Tiles
{
    public interface ITile
    {
        TileType Type { get; }

        Building Building { get; set; }

        Unit Unit { get; set; }
    }
}