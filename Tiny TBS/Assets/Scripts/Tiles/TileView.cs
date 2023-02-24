using System;
using Assets.Scripts.Buildings;
using Assets.Scripts.Configs;
using Assets.Scripts.Units;
using UnityEngine;

namespace Assets.Scripts.Tiles
{
    public class TileView : MonoBehaviour, ITile
    {
        private TilesConfig _tilesConfig;
        public TileType Type { get; private set; }

        public Building Building
        {
            get => _building;
            set
            {
                _building = value;
                SetBuilding(value);
            }
    }

        public Unit Unit { get; set; }

        private Building _building;
        private GameObject _buildingObject;

        private void Awake()
        {
            _tilesConfig = TilesConfig.instance;
        }

        public void SetType(TileType type)
        {
            Type = type;
        }

        private void SetBuilding(Building building)
        {
            GameObject prefab = null;
            switch (building.Type)
            {
                case BuildingType.None:
                    break;

                case BuildingType.Village:
                    prefab = _tilesConfig.buildingPrefabs.village;
                    break;

                case BuildingType.Castle:
                    prefab = _tilesConfig.buildingPrefabs.castle;
                    break;
            }

            if (_buildingObject != null)
            {
                Destroy(_buildingObject);
            }

            if (prefab != null)
            {
                _buildingObject = Instantiate(prefab, transform);
                _buildingObject.GetComponent<FractionColorCustomizer>()
                    ?.Init(building.Fraction);
            }
        }
    }
}
