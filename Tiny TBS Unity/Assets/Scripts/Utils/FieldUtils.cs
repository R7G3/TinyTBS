using System.Collections.Generic;
using UnityEngine;

namespace Utils
{
    public static class FieldUtils
    {
        private static Plane _fieldPlane = new Plane(Vector3.up, 0); // zero on y
        
        public static Vector2Int GetCoordFromMousePos(Vector3 mousePosition, Camera camera)
        {
            var ray = camera.ScreenPointToRay(mousePosition);

            _fieldPlane.Raycast(ray, out var distance);
            var worldPosition = ray.GetPoint(distance);
            
            return To2dCoord(worldPosition);
        }

        public static Vector3 GetWorldPos(Vector2Int coord)
        {
            return new Vector3(coord.x, 0, coord.y);
        }

        public static Vector2Int To2dCoord(Vector3 vector)
        {
            return new Vector2Int(
                Mathf.RoundToInt(vector.x),
                Mathf.RoundToInt(vector.z));
        }
    }
}