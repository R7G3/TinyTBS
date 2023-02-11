using UnityEngine;

namespace Assets.Scripts
{
    public class Map
    {
        private TileView[,] _tiles;

        public Map(TileView[,] tiles)
        {
            _tiles = tiles;
        }

        public Map(int x, int y)
        {
            _tiles = new TileView[x, y];
        }

        public TileView this[int x, int y]
        {
            get
            {
                return _tiles[x, y];
            }
            set
            {
                _tiles[x, y] = value;
            }
        }

        public TileView this[Vector2Int coord]
        {
            get
            {
                return _tiles[coord.x, coord.y];
            }
            set
            {
                _tiles[coord.x, coord.y] = value;
            }
        }
    }
}
