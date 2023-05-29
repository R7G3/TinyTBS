using UnityEngine;

namespace Assets.Scripts.Buildings
{
    public class Building : IGameplayObject
    {
        public BuildingType Type { get; set; }

        public BuildingState State { get; set; }

        public Player Owner { get; set; }

        public Vector2Int Coord { get; set; }

        public bool IsEnabled { get; set; } = true;
    }
}
