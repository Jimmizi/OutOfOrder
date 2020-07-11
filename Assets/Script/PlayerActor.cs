using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : GridActor
{
    void Awake()
    {
        Service.Player = this;

        var camFollow = Camera.main.GetComponent<CameraFollow>();
        if (camFollow)
        {
            camFollow.ManuallySetFollowPlayer();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        Service.Flow.ObjectsToDestroyOnLevelEnd.Add(this.gameObject);
    }

    bool IsDirectionFreeToMoveIn(Vector2 dir)
    {
        return true;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, GridManager.TILE_SIZE);
        return hit.collider == null;
    }

    void InteractNearbyObject()
    {
        if (Service.Grid.ToggleNearestDoor(GetGridPosition()))
        {
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Service.Flow.IsGameRunning)
        {
            return;
        }

        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
        {
            if(IsDirectionFreeToMoveIn(Vector2.up))
            {
                transform.position += new Vector3(0, MoveSpeed * GameConfig.GetDeltaTime());
            }
        }
        else if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
        {
            if (IsDirectionFreeToMoveIn(Vector2.down))
            {
                transform.position += new Vector3(0, -MoveSpeed * GameConfig.GetDeltaTime());
            }
        }

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            if (IsDirectionFreeToMoveIn(Vector2.left))
            {
                transform.position += new Vector3(-MoveSpeed * GameConfig.GetDeltaTime(), 0);
            }
        }
        else if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            if (IsDirectionFreeToMoveIn(Vector2.right))
            {
                transform.position += new Vector3(MoveSpeed * GameConfig.GetDeltaTime(), 0);
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return))
        {
            InteractNearbyObject();
        }

    }

    
}
