using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Tilemaps;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
    public const int INVALID_TILE_VAL = -9999;

    public const float TILE_SIZE = 1f;

    public static Vector2Int INVALID_TILE = new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL);

    public struct PathFindOptions
    {
        public bool IgnoreCollision;
    }

    /// <summary>
    /// Tilemap given by the editor
    /// </summary>
    public Tilemap EditorTilemap;

    // Min and max bounds of the playable grid
    private Vector2Int minBounds;
    private Vector2Int maxBounds;

    /// <summary>
    /// Current tilelist for this level
    /// </summary>
    private TileBase[] tileList = null;

    /// <summary>
    /// Hard collision that isn't going to change at runtime
    /// </summary>
    private Dictionary<int, Dictionary<int, bool>> tileHardCollision;

    /// <summary>
    /// List of valid tiles that can actually be moved to (within bounds)
    /// </summary>
    private List<Vector2Int> validTileList = new List<Vector2Int>();

    private bool DoesTileNameHaveCollision(string tileName)
    {
        switch (tileName)
        {
            case "Debug_WallSprite":
                return true;

            default:
                return false;
        }
    }

    /// <summary>
    /// Cache all the hard (static) collision on the level
    /// </summary>
    private void CacheCollision()
    {
        validTileList.Clear();
        tileHardCollision = new Dictionary<int, Dictionary<int, bool>>();

        for (var x = minBounds.x; x < maxBounds.x; x++)
        {
            for (var y = minBounds.y; y < maxBounds.y; y++)
            {
                var t = EditorTilemap.GetTile(new Vector3Int(x, y, 0));

                if (!tileHardCollision.ContainsKey(x))
                {
                    tileHardCollision.Add(x, new Dictionary<int, bool>());
                }

                if (!tileHardCollision[x].ContainsKey(y))
                {
                    tileHardCollision[x].Add(y, false);
                }

                tileHardCollision[x][y] = t == null || DoesTileNameHaveCollision(t.name);

                if (!tileHardCollision[x][y])
                {
                    validTileList.Add(new Vector2Int(x+1,y+1));
                }
            }
        }
    }

    void Awake()
    {
        Service.Grid = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        var bounds = EditorTilemap.cellBounds;

        minBounds = new Vector2Int(bounds.min.x, bounds.min.y);
        maxBounds = new Vector2Int(bounds.max.x, bounds.max.y);

        tileList = EditorTilemap.GetTilesBlock(bounds);


        CacheCollision();
    }

    public bool HasGridLos(Vector2Int origin, Vector2Int destination)
    {
        PathFindOptions opt = new PathFindOptions
        {
            IgnoreCollision = true
        };

        var path = GetPath(origin, destination, opt);
        return !DoesPathContainCollision(path);
    }

    public Path GetPath(Vector2Int origin, Vector2Int destination)
    {
        return GetPath(origin, destination, new PathFindOptions());
    }

    public Path GetPath(Vector2Int origin, Vector2Int destination, PathFindOptions opt)
    {
        List<Vector2Int> returnPath = new List<Vector2Int>();
        Vector2Int nextLowestScoredTile = new Vector2Int();


        List<Vector2Int> openList = new List<Vector2Int>(), closedList = new List<Vector2Int>();
        Dictionary<Vector2Int, float> movementCost = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, float> scores = new Dictionary<Vector2Int, float>();
        Dictionary<Vector2Int, Vector2Int> parent = new Dictionary<Vector2Int, Vector2Int>();

        closedList.Add(origin);
        parent.Add(origin, origin);
        movementCost.Add(origin, 0);

        Vector2Int GetOpenTileWithLowestScore()
        {
            float lowestScore = 999999f;
            Vector2Int tile = new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL);

            foreach (var t in openList)
            {
                if (scores.ContainsKey(t) && scores[t] < lowestScore)
                {
                    lowestScore = scores[t];
                    tile = t;
                }
            }

            return tile;
        }

        void CalculateScore(Vector2Int point)
        {
            var distToDest = GetManhattanDistance(point, destination);
            scores.Add(point, distToDest + movementCost[point]);
        }

        void AddNeighboursToOpenList(Vector2Int point)
        {
            void AddPoint(Vector2Int p)
            {
                if (openList.Contains(p))
                {
                    return;
                }

                if (closedList.Contains(p))
                {
                    return;
                }

                if (!opt.IgnoreCollision)
                {
                    // Not a valid walkable tile
                    if (!validTileList.Contains(p))
                    {
                        return;
                    }
                }
                
                openList.Add(p);
                parent.Add(p, point);

                movementCost.Add(p, movementCost[point] + 1);

                CalculateScore(p);
            }

            if (point.x > minBounds.x)
            {
                // Add to the left if not at the min bounds
                AddPoint(new Vector2Int(point.x - 1, point.y));
            }

            if (point.x < maxBounds.x)
            {
                // Add to the right if not at the min bounds
                AddPoint(new Vector2Int(point.x + 1, point.y));
            }

            if (point.y > minBounds.y)
            {
                // Add to the below if not at the min bounds
                AddPoint(new Vector2Int(point.x, point.y - 1));
            }

            if (point.y < maxBounds.y)
            {
                // Add to the above if not at the min bounds
                AddPoint(new Vector2Int(point.x, point.y + 1));
            }
        }

        AddNeighboursToOpenList(origin);

        // While we haven't reached the destination and the next point to look at isn't invalid
        while (nextLowestScoredTile != destination &&
               nextLowestScoredTile != new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL))
        {
            nextLowestScoredTile = GetOpenTileWithLowestScore();

            if (nextLowestScoredTile == destination ||
                nextLowestScoredTile == new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL))
            {
                continue;
            }

            closedList.Add(nextLowestScoredTile);
            openList.Remove(nextLowestScoredTile);

            AddNeighboursToOpenList(nextLowestScoredTile);
        }

        if (nextLowestScoredTile == destination)
        {
            returnPath.Add(nextLowestScoredTile);
            Vector2Int parentNext = parent[nextLowestScoredTile];

            while (parentNext != origin && parentNext != new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL))
            {
                returnPath.Add(parentNext);
                parentNext = parent[parentNext];
            }

            returnPath.Add(origin);
            returnPath.Reverse();
        }

        return new Path(returnPath);
    }

    public bool DoesPathContainCollision(Path path)
    {
        foreach (var tile in path.GridPoints)
        {
            if(!validTileList.Contains(new Vector2Int(tile.x, tile.y)))
            {
                return true;
            }
        }

        return false;
    }

    #region Utility

    public int GetManhattanDistance(Vector2Int a, Vector2Int b)
    {
        var positionDifference = a - b;
        positionDifference.x = Math.Abs(positionDifference.x);
        positionDifference.y = Math.Abs(positionDifference.y);

        return positionDifference.x + positionDifference.y;
    }

    /// <summary>
    /// Get a random position on the playable grid area
    /// </summary>
    public Vector2Int GetRandomPositionOnGrid()
    {
        int randomElement = Random.Range(0, validTileList.Count);
        return validTileList[randomElement];
    }

    public bool IsNextPointFreeOfNpcs(Path path)
    {
        if (path.CurrentPoint >= path.Size)
        {
            return false;
        }

        if (IsActorOnPoint(path.GridPoints[path.CurrentPoint]))
        {
            return false;
        }

        if (path.CurrentPoint < path.Size - 1)
        {
            if (IsActorOnPoint(path.GridPoints[path.CurrentPoint + 1]))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsPathFreeOfNpcs(Path path)
    {
        for (var i = path.CurrentPoint; i < path.GridPoints.Count; i++)
        {
            var point = path.GridPoints[i];
            if (IsActorOnPoint(point))
            {
                return false;
            }
        }

        return true;
    }

    public bool IsActorOnPoint(Vector2Int point)
    {
        foreach (var actor in GridActor.ActorList)
        {
            if (actor.GetGridPosition() == point)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="minDist">Inclusive min distance</param>
    /// <param name="maxDist">Inclusive max distance</param>
    /// <returns></returns>
    public Vector2Int GetPositionNearby(Vector2Int origin, int minDist = 2, int maxDist = -1)
    {
        List<Vector2Int> validPoints = new List<Vector2Int>();

        for (var i = 0; i < validTileList.Count; i++)
        {
            Vector2Int pos = validTileList[i];

            var dist = GetManhattanDistance(origin, pos);

            if (maxDist != -1)
            {
                // Too far
                if (dist > maxDist)
                {
                    continue;
                }
            }

            // Too close
            if (dist < minDist)
            {
                continue;
            }

            if (!IsPointValid(pos))
            {
                continue;
            }

            validPoints.Add(pos);
        }

        return validPoints.GetRandom();
    }

    public bool IsPointValid(Vector2Int point, bool skipInValidListCheck = false)
    {
        if (!skipInValidListCheck && !validTileList.Contains(point))
        {
            return false;
        }

        foreach (var actor in GridActor.ActorList)
        {
            if (actor.GetGridPosition() == point)
            {
                return false;
            }
        }

        return true;
    }

    #endregion

    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        for (int x = minBounds.x; x < maxBounds.x; x++)
        {
            for (int y = minBounds.y; y < maxBounds.y; y++)
            {
                if (validTileList.Contains(new Vector2Int(x, y)))
                {
                    Gizmos.color = new Color(0, 1, 0, 0.25f);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0.25f);
                }

                Gizmos.DrawCube(new Vector3(x, y, 0), new Vector3(1f, 1f, 1f));
            }
        }
    }
}
