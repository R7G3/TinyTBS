using UnityEngine;

namespace Utils
{
    public static class FieldUtils
    {
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