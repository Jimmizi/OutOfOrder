using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MyExtensions
{
    public static Vector2Int GetRandom(this List<Vector2Int> list)
    {
        if (list.Count == 0)
        {
            return new Vector2Int(GridManager.INVALID_TILE_VAL, GridManager.INVALID_TILE_VAL);
        }

        int rand = Random.Range(0, list.Count);

        return list[rand];
    }
}
