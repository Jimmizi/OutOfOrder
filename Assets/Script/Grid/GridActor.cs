using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridActor : MonoBehaviour
{
    private const float StopChanceForNpcsInfront = 50f;

    public static List<GridActor> ActorList = new List<GridActor>();

    public bool UseNonLinearMovement = true;

    protected bool AreOtherEnemiesAffectingPlayerWithMyPower()
    {
        GridEnemy me = this as GridEnemy;

        if (me == null)
        {
            return false;
        }

        foreach (var actor in ActorList)
        {
            if (actor is GridEnemy enemy)
            {
                if (enemy == this)
                {
                    continue;
                }

                if (enemy.PowerStyle != me.PowerStyle)
                {
                    continue;
                }

                if (!enemy.IsAffectingPlayerWithPower)
                {
                    continue;
                }

                return true;
            }
        }

        return false;
    }

    public static void TellEnemiesToLosePlayer()
    {
        foreach (var actor in ActorList)
        {
            if (actor is GridEnemy enemy)
            {
                enemy.ImmediatelyLosePlayer();
            }
        }
    }

    public static bool GetDistanceToClosestEnemy(Vector2 point, out float nearestDist, out float powerAffectDist, bool onlyGlitchEnemies = false)
    {
        nearestDist = 999999999f;
        powerAffectDist = 10f;

        foreach (var actor in ActorList)
        {
            if (actor is GridEnemy enemy)
            {
                var dist = Vector2.Distance(point, enemy.GetWorldPosition());

                if (onlyGlitchEnemies)
                {
                    if (enemy.PowerStyle != GridEnemy.EnemyPower.GlitchScreen)
                    {
                        continue;
                    }

                    powerAffectDist = enemy.PowerAffectDistance;

                    if (dist >= enemy.PowerAffectDistance)
                    {
                        continue;
                    }
                }

                if (dist < nearestDist)
                {
                    nearestDist = dist;
                }

            }
        }

        return Math.Abs(nearestDist - 999999999f) > 1f;
    }

    public static int GetNumberOfEnemies()
    {
        int numEnemies = 0;

        foreach (var actor in ActorList)
        {
            if (actor.tag == "enemy")
            {
                numEnemies++;
            }
        }

        return numEnemies;
    }

    public static int GetNumberOfActorsChasing()
    {
        int numChasing = 0;

        foreach (var actor in ActorList)
        {
            if (actor is GridEnemy enemy)
            {
                if (enemy.IsChasing)
                {
                    numChasing++;
                }
            }
        }

        return numChasing;
    }

    public float MoveSpeed = 2.0f;

    // Threshold for being at a specific cell
    public const float PATH_POINT_THRESHOLD = 0.2f;

    // Start is called before the first frame update
    public virtual void Start()
    {
        ActorList.Add(this);
    }

    public virtual void OnDestroy()
    {
        ActorList.Remove(this);
    }

    // Update is called once per frame
    public virtual void Update()
    {
        
    }

    public Vector2Int GetGridPosition()
    {
        var offset = new Vector2();

        if (tag == "Player" || tag == "Enemy" || tag == "player" || tag == "enemy")
        {
            offset = new Vector2(0.5f, 0.5f);
        }

        var gridPos = new Vector2Int((int) Mathf.Round(transform.position.x + offset.x), (int) Mathf.Round(transform.position.y + offset.y));
        gridPos = Service.Grid.FindNearestValidTile(gridPos);

        return gridPos;
    }

    public Vector2 GetWorldPosition()
    {
        var offset = new Vector2();

        if (tag == "Player" || tag == "Enemy" || tag == "player" || tag == "enemy")
        {
            offset = new Vector2(0.5f, 0.5f);
        }

        return new Vector2(transform.position.x + offset.x, transform.position.y + offset.y);
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

        var targetPosition = path.GetNextWorldPosition();
        
        // Have completed the path
        if (Vector2.Distance(GetWorldPosition(), path.GetEndPosition()) < PATH_POINT_THRESHOLD)
        {
            return true;
        }

        if (targetPosition == GridManager.INVALID_TILE)
        {
            return true;
        }

        if (Vector2.Distance(GetWorldPosition(), targetPosition) < PATH_POINT_THRESHOLD)
        {
            path.IncrementPoint();
            targetPosition = path.GetNextWorldPosition();

            if (targetPosition == GridManager.INVALID_TILE)
            {
                return true;
            }

            if (quitIfPathIsBlocked && !Service.Grid.IsNextPointFreeOfNpcs(path))
            {
                // 50 - 50 chance of stopping when another npc in in front of them on the path
                if (Random.Range(0f, 100f) < StopChanceForNpcsInfront)
                {
                    return true;
                }
            }

            if (Service.Grid.DoesTileContainAClosedDoor(path.GetNextGridPosition())
                || Service.Grid.DoesTileContainAClosedDoor(path.GetNextGridPosition(1)))
            {
                return true;
            }
        }

        if (!UseNonLinearMovement)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, GameConfig.GetDeltaTime() * MoveSpeed);
        }
        else
        {
            var heading = targetPosition - GetWorldPosition();
            var dist = heading.magnitude;
            var dir = heading / dist;

            var moveDir = new Vector3(dir.x, dir.y, 0);

            transform.position += moveDir * (GameConfig.GetDeltaTime() * MoveSpeed);
        }

        return false;
    }
}
