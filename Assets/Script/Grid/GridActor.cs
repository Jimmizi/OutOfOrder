using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridActor : MonoBehaviour
{
    public static List<GridActor> ActorList = new List<GridActor>();

    public float MoveSpeed = 2.0f;

    // Threshold for being at a specific cell
    public const float PATH_POINT_THRESHOLD = 0.25f;

    // Start is called before the first frame update
    void Start()
    {
        ActorList.Add(this);
    }

    void OnDestroy()
    {
        ActorList.Remove(this);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int((int) Mathf.Round(transform.position.x), (int) Mathf.Round(transform.position.y));
    }

    public Vector2 GetWorldPosition()
    {
        return new Vector2(transform.position.x, transform.position.y);
    }

    /// <summary>
    /// Moves the actor along the passed in path.
    /// </summary>
    /// <returns>True when at the end of the path</returns>
    public bool TaskMoveAlongPath(ref Path path, bool quitIfPathIsBlocked = true)
    {
        if (path == null)
        {
            return false;
        }

        // Have completed the path
        if (Vector2.Distance(GetWorldPosition(), path.GetEndPosition()) < PATH_POINT_THRESHOLD)
        {
            return true;
        }

        var nextPos = path.GetNextWorldPosition();

        if (Vector2.Distance(GetWorldPosition(), nextPos) < PATH_POINT_THRESHOLD)
        {
            path.IncrementPoint();
            nextPos = path.GetNextWorldPosition();

            if (quitIfPathIsBlocked && !Service.Grid.IsNextPointFreeOfNpcs(path))
            {
                return true;
            }
        }

        transform.position = Vector3.Lerp(transform.position, nextPos, GameConfig.GetDeltaTime() * MoveSpeed);

        return false;
    }
}
