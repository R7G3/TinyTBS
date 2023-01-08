namespace Assets.Scripts
{
    public class Map
    {
        private Tile[,] _tiles;

        public Map(Tile[,] tiles)
        {
            _tiles = tiles;
        }

        public Tile this[int x, int y]
        {
            get
            {
                return _tiles[x, y];
            }
        }
    }
}
