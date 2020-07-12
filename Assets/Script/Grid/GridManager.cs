using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class GridManager : MonoBehaviour
{
#if UNITY_EDITOR
    public bool DrawDebug = false;
#endif

    public TileBase HorizontalFloorTile;
    public TileBase VerticalFloorTile;
    public TileBase FloorTileGeneric;

    public GameObject PlayerPrefab;
    public GameObject LevelGoalPrefab;
    public GameObject HorizontalDoorPrefab;
    public GameObject VerticalDoorPrefab;

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
    public Tilemap EditorTilemapRef;

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

    /// <summary>
    /// List of doors on tiles, if locked, dictionary second element will be true
    /// </summary>
    private Dictionary<Vector2Int, bool> doorTileList = new Dictionary<Vector2Int, bool>();
    private Dictionary<Vector2Int, Door> doorDataToObjectList = new Dictionary<Vector2Int, Door>();
    private Dictionary<Vector2Int, Vector2Int> doorTileLinks = new Dictionary<Vector2Int, Vector2Int>();

    public Vector2Int FindNearestValidTile(Vector2Int point)
    {
        if (validTileList.Contains(point))
        {
            return point;
        }

        // Check left
        if (validTileList.Contains(point + new Vector2Int(-1, 0)))
        {
            return point + new Vector2Int(-1, 0);
        }

        // Check right
        if (validTileList.Contains(point + new Vector2Int(1, 0)))
        {
            return point + new Vector2Int(1, 0);
        }

        // Check up
        if (validTileList.Contains(point + new Vector2Int(0, 1)))
        {
            return point + new Vector2Int(0, 1);
        }

        // Check down
        if (validTileList.Contains(point + new Vector2Int(0, -1)))
        {
            return point + new Vector2Int(0, -1);
        }

        // Diagonals

        // Check left top
        if (validTileList.Contains(point + new Vector2Int(-1, 1)))
        {
            return point + new Vector2Int(-1, 1);
        }

        // Check right top
        if (validTileList.Contains(point + new Vector2Int(1, 1)))
        {
            return point + new Vector2Int(1, 1);
        }

        // Check left bot
        if (validTileList.Contains(point + new Vector2Int(-1, -1)))
        {
            return point + new Vector2Int(-1, -1);
        }

        // Check right bot
        if (validTileList.Contains(point + new Vector2Int(1, -1)))
        {
            return point + new Vector2Int(1, -1);
        }

#if DEBUG
        Debug.LogError("Managed to test against a tile nowhere on the valid list");
#endif

        return GetRandomPositionOnGrid();
    }

    private bool DoesTileNameHaveCollision(string tileName)
    {
        if (tileName.Contains("Fake") || tileName.Contains("fake"))
        {
            return true;
        }

        if (!tileName.Contains("Floor") && !tileName.Contains("floor"))
        {
            return true;
        }

        return false;

        //switch (tileName)
        //{
        //    case "Debug_WallSprite":
        //        return true;

        //    default:
        //        return false;
        //}
    }

    private bool IsInvisibleCollision(string tileName)
    {
        return tileName == "InvisibleCollision";
    }

    private bool IsTileNameADoor(string tileName)
    {
        switch (tileName)
        {
            case "Debug_DoorSprite":
                return true;

            default:
                return false;
        }
    }

    private bool IsTileNamePlayerSpawn(string tileName)
    {
        switch (tileName)
        {
            case "Debug_PlayerSprite":
                return true;

            default:
                return false;
        }
    }

    private bool IsTileNameLevelGoalSpawn(string tileName)
    {
        switch (tileName)
        {
            case "Debug_LevelGoal":
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
                var t = EditorTilemapRef.GetTile(new Vector3Int(x, y, 0));

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

                if (t == null)
                {
                    continue;
                }

                var gridPos = new Vector2Int(x + 1, y + 1);

                if (IsTileNameADoor(t.name))
                {
                    doorTileList.Add(gridPos, false); // open
                    
                    var leftTile = EditorTilemapRef.GetTile(new Vector3Int(x - 1, y, 0));
                    var rightTile = EditorTilemapRef.GetTile(new Vector3Int(x + 1, y, 0));
                    var upwardsTile = EditorTilemapRef.GetTile(new Vector3Int(x, y + 1, 0));
                    var downwardsTile = EditorTilemapRef.GetTile(new Vector3Int(x, y - 1, 0));

                    GameObject doorGo = null;

                    if (leftTile && rightTile && upwardsTile && downwardsTile)
                    {
                        // Is it vertical?
                        if (DoesTileNameHaveCollision(upwardsTile.name) && DoesTileNameHaveCollision(downwardsTile.name))
                        {
                            doorGo = (GameObject)Instantiate(VerticalDoorPrefab, new Vector3(gridPos.x - 0.5f, gridPos.y - 0.5f, 0), Quaternion.identity);
                            EditorTilemapRef.SetTile(new Vector3Int(gridPos.x - 1, gridPos.y - 1, 0), VerticalFloorTile);
                        }
                        else // Otherwise horizontal
                        {
                            doorGo = (GameObject)Instantiate(HorizontalDoorPrefab, new Vector3(gridPos.x - 0.5f, gridPos.y - 0.5f, 0), Quaternion.identity);
                            EditorTilemapRef.SetTile(new Vector3Int(gridPos.x - 1, gridPos.y - 1, 0), HorizontalFloorTile);
                        }

                        tileHardCollision[x][y] = false;
                        validTileList.Add(new Vector2Int(x + 1, y + 1));
                    }

                    if (doorTileList.Count % 2 == 0)
                    {
                        doorTileList[gridPos] = true; //closed

                        //Link the current "true" door to the previous "false" door
                        doorTileLinks.Add(doorTileList.ElementAt(doorTileList.Count - 1).Key, doorTileList.ElementAt(doorTileList.Count - 2).Key);
                        doorTileLinks.Add(doorTileList.ElementAt(doorTileList.Count - 2).Key, doorTileList.ElementAt(doorTileList.Count - 1).Key);
                    }

                    if (doorGo != null)
                    {
                        var doorComp = doorGo.GetComponent<Door>();
                        if (doorComp)
                        {
                            doorComp.isOpen = !doorTileList[gridPos];
                            doorDataToObjectList.Add(gridPos, doorComp);
                        }
                    }
                }
                else if (IsTileNamePlayerSpawn(t.name))
                {
                    EditorTilemapRef.SetTile(new Vector3Int(gridPos.x - 1, gridPos.y - 1, 0), FloorTileGeneric);
                    tileHardCollision[x][y] = false;
                    validTileList.Add(new Vector2Int(x + 1, y + 1));

                    var playerGo = (GameObject) Instantiate(PlayerPrefab, new Vector3(gridPos.x - 0.5f, gridPos.y - 0.5f, 0), Quaternion.identity);
                }
                else if (IsTileNameLevelGoalSpawn(t.name))
                {
                    EditorTilemapRef.SetTile(new Vector3Int(gridPos.x - 1, gridPos.y - 1, 0), FloorTileGeneric);
                    tileHardCollision[x][y] = false;
                    validTileList.Add(new Vector2Int(x + 1, y + 1));

                    var playerGo = (GameObject)Instantiate(LevelGoalPrefab, new Vector3(gridPos.x, gridPos.y, 0), Quaternion.identity);
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
        if (EditorTilemapRef == null)
        {
            EditorTilemapRef = GetComponent<Tilemap>();
        }

        var bounds = EditorTilemapRef.cellBounds;

        minBounds = new Vector2Int(bounds.min.x, bounds.min.y);
        maxBounds = new Vector2Int(bounds.max.x, bounds.max.y);

        tileList = EditorTilemapRef.GetTilesBlock(bounds);

        CacheCollision();
    }

    public bool ToggleNearestDoor(Vector2 nearPos)
    {
        float bestDist = 9999f;
        Vector2Int nearestDoor = INVALID_TILE;

        foreach (var door in doorTileList)
        {
            var dist = Vector2.Distance(door.Key, nearPos);

            if (dist < bestDist && dist > TILE_SIZE * 0.15f)
            {
                bestDist = dist;
                nearestDoor = door.Key;
            }
        }

        // Times 1.5f for a little distance buffer
        if (bestDist < TILE_SIZE * 1.85f)
        {
            // Toggle the state of the nearest door
            doorTileList[nearestDoor] = !doorTileList[nearestDoor];

            if (doorDataToObjectList.ContainsKey(nearestDoor))
            {
                doorDataToObjectList[nearestDoor].isOpen = !doorTileList[nearestDoor];
            }

            // Toggle the nearest doors linked door and do the same
            if (doorTileLinks.ContainsKey(nearestDoor) && doorTileList.ContainsKey(doorTileLinks[nearestDoor]))
            {
                doorTileList[doorTileLinks[nearestDoor]] = !doorTileList[doorTileLinks[nearestDoor]];

                if (doorDataToObjectList.ContainsKey(doorTileLinks[nearestDoor]))
                {
                    doorDataToObjectList[doorTileLinks[nearestDoor]].isOpen = !doorTileList[doorTileLinks[nearestDoor]];
                }
            }

            return true;
        }

        return false;
    }

    public bool HasGridLos(Vector2Int origin, Vector2Int destination)
    {
        if (origin == INVALID_TILE || destination == INVALID_TILE)
        {
            return false;
        }

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
        Vector2Int nextLowestScoredTile = INVALID_TILE;


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

                    if (DoesTileContainAClosedDoor(p))
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
        while (nextLowestScoredTile != destination)
        {
            nextLowestScoredTile = GetOpenTileWithLowestScore();

            if (nextLowestScoredTile == destination ||
                nextLowestScoredTile == new Vector2Int(INVALID_TILE_VAL, INVALID_TILE_VAL))
            {
                break;
            }

            closedList.Add(nextLowestScoredTile);
            openList.Remove(nextLowestScoredTile);

            AddNeighboursToOpenList(nextLowestScoredTile);
        }

        if (nextLowestScoredTile == destination)
        {
            if (!parent.ContainsKey(nextLowestScoredTile))
            {
                return new Path(null);
            }

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

            if (doorTileList.ContainsKey(tile) && doorTileList[tile])
            {
                return true;
            }
        }

        return false;
    }

    public bool DoesTileContainADoor(Vector2Int point)
    {
        return doorTileList.ContainsKey(point);
    }

    public bool DoesTileContainAClosedDoor(Vector2Int point)
    {
        return doorTileList.ContainsKey(point) && doorTileList[point];
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

    public struct GetPositionOptions
    {
        public List<Vector2Int> AvoidancePoints;
        public float AvoidanceDistance;

        public bool IgnorePositionedActors;
        public bool AddEnemiesToBeAvoided;
        public bool AddPelletsToBeAvoided;
    }
    
    public Vector2Int GetPositionOnGrid(GetPositionOptions opt)
    {
        Dictionary<Vector2Int, float> tileScores = new Dictionary<Vector2Int, float>();

        foreach (var point in validTileList)
        {
            float score = Random.Range(0.9f, 1.1f);

            if (!opt.IgnorePositionedActors && IsActorOnPoint(point))
            {
                continue;
            }

            if (DoesTileContainADoor(point))
            {
                continue;
            }

            foreach(var avdPoint in opt.AvoidancePoints)
            {
                var dist = Vector2.Distance(avdPoint, point);
                score += dist < opt.AvoidanceDistance ? dist : opt.AvoidanceDistance;
            }

            if (opt.AddPelletsToBeAvoided)
            {
                foreach (var pellet in ScorePellet.PelletList)
                {
                    var pos = pellet.GetGridPosition();

                    var dist = Vector2.Distance(pos, point);
                    score += dist < opt.AvoidanceDistance ? dist : opt.AvoidanceDistance;
                }
            }

            if (opt.AddEnemiesToBeAvoided)
            {
                foreach (var actor in GridActor.ActorList)
                {
                    if (actor.tag != "enemy")
                    {
                        continue;
                    }

                    var pos = actor.GetGridPosition();

                    var dist = Vector2.Distance(pos, point);
                    score += dist < opt.AvoidanceDistance ? dist : opt.AvoidanceDistance;
                }
            }

            tileScores.Add(point, score);
        }

        float highestScore = 1f;
        Vector2Int bestTile = INVALID_TILE;

        foreach (var score in tileScores)
        {
            if (bestTile == INVALID_TILE || score.Value > highestScore)
            {
                highestScore = score.Value;
                bestTile = score.Key;
            }
        }

        return bestTile;
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
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (!DrawDebug)
        {
            return;
        }
        foreach(var door in doorTileList)
        {
            if (door.Value)
            {
                Gizmos.color = new Color(1, 0, 0, 1f);
            }
            else
            {
                Gizmos.color = new Color(0, 1, 0, 1f);
            }

            Gizmos.DrawCube(new Vector3(door.Key.x, door.Key.y, 0), new Vector3(0.25f, 0.25f, 0.25f));
        }

        foreach (var doorLink in doorTileLinks)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawLine(new Vector3(doorLink.Key.x, doorLink.Key.y), new Vector3(doorLink.Value.x, doorLink.Value.y));
        }

        for (int x = minBounds.x; x < maxBounds.x; x++)
        {
            for (int y = minBounds.y; y < maxBounds.y; y++)
            {
                if (validTileList.Contains(new Vector2Int(x, y)))
                {
                    Gizmos.color = new Color(0, 1, 0, 0.15f);
                }
                else
                {
                    Gizmos.color = new Color(1, 0, 0, 0.15f);
                }

                Gizmos.DrawCube(new Vector3(x, y, 0), new Vector3(1f, 1f, 1f));
            }
        }
    }
#endif
}
