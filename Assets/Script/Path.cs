using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Path
{
    public Path(List<Vector2Int> points)
    {
        GridPoints = points;
        CurrentPoint = 0;
    }

    public List<Vector2Int> GridPoints;
    public int CurrentPoint;

    public int Size => GridPoints?.Count ?? 0;
    public bool IsValid => Size != 0;

    public void IncrementPoint()
    {
        CurrentPoint++;
    }

    public void UpdateProgression(Vector2 currentPosition)
    {
        if (Size <= 1)
        {
            return;
        }

        // No point if the current point is the final point
        if (CurrentPoint == Size - 1)
        {
            return;
        }

        for (int i = 1; i < GridPoints.Count; i++)
        {
            float distToPrevious = Vector2.Distance(currentPosition, GridPoints[i - 1]);

            if (distToPrevious > GridManager.TILE_SIZE)
            {
                continue;
            }

            CurrentPoint = i;
            return;
        }
    }

    public Vector2 GetNextWorldPosition()
    {
        if (!IsValid)
        {
            return GridManager.INVALID_TILE;
        }

        if (CurrentPoint >= Size)
        {
            return new Vector2(GridPoints[Size - 1].x, GridPoints[Size - 1].y);
        }

        return new Vector2(GridPoints[CurrentPoint].x, GridPoints[CurrentPoint].y);
    }

    public Vector2Int GetNextGridPosition(int offset = 0)
    {
        if (CurrentPoint + offset >= Size)
        {
            return new Vector2Int(GridPoints[Size - 1].x, GridPoints[Size - 1].y);
        }

        return new Vector2Int(GridPoints[CurrentPoint + offset].x, GridPoints[CurrentPoint + offset].y);
    }

    public Vector2Int GetEndPosition()
    {
        if (!IsValid)
        {
            return new Vector2Int(GridManager.INVALID_TILE_VAL, GridManager.INVALID_TILE_VAL);
        }

        return GridPoints[Size - 1];
    }

}
