using Assets.Scripts.Buildings;
using Assets.Scripts.Units;

namespace Assets.Scripts.Tiles
{
    public class Tile
    {
        public TileType Type { get; set; }

        public Building Building { get; set; }

        public Unit Unit { get; set; }
    }
}