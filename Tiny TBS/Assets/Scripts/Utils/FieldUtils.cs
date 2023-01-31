using UnityEngine;

namespace Utils
{
    public static class FieldUtils
    {
        public static Vector3 GetWorldPos(Vector2Int coord)
        {
            return new Vector3(coord.x, 0, coord.y);
        }
    }
}