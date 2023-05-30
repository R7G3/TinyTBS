using Assets.Scripts.Tiles;
using UnityEngine;

namespace Assets.Scripts
{
    public class Map
    {
        private ITile[,] _tiles;

        public Map(ITile[,] tiles)
        {
            _tiles = tiles;
        }

        public Map(int x, int y)
        {
            _tiles = new TileView[x, y];
        }

        public bool IsValidCoord(Vector2Int coord)
            => coord.x >= 0 && coord.x < _tiles.GetLength(Definitions.xAxis) &&
               coord.y >= 0 && coord.y < _tiles.GetLength(Definitions.yAxis);

        public ITile this[int x, int y]
        {
            get { return _tiles[x, y]; }
            set { _tiles[x, y] = value; }
        }

        public ITile this[Vector2Int coord]
        {
            get { return _tiles[coord.x, coord.y]; }
            set { _tiles[coord.x, coord.y] = value; }
        }

        public int GetSizeByAxis(int axis)
        {
            return _tiles.GetLength(axis);
        }
    }
}