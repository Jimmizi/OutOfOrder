using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : GridActor
{
    SpriteRenderer _renderer;
    SpriteRenderer renderer
    {
        get
        {
            if(!_renderer)
            {
                _renderer = GetComponent<SpriteRenderer>();
            }

            return _renderer;
        }
    }

    public bool AreControlsConfused = false;
    public bool IsBeingSlowed = false;

    public Sprite leftSprite;
    public Sprite rightSprite;
    public Sprite upSprite;
    public Sprite downSprite;

    public float SlowSpeed = 1f;

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
        Vector3 offset = new Vector3(0.5f, 0.5f, 0f);

        RaycastHit2D hit = Physics2D.Raycast(transform.position + offset, dir, GridManager.TILE_SIZE / 2);
        return hit.collider == null || hit.collider.name != "BG_Tiles";
    }

    void InteractNearbyObject()
    {
        if (Service.Grid.ToggleNearestDoor(GetWorldPosition()))
        {
            return;
        }
    }

    bool IsLeftPressed()
    {
        if (AreControlsConfused)
        {
            // Now press up
            return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        }
        
        return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
    }

    bool IsRightPressed()
    {
        if (AreControlsConfused)
        {
            // Now need to press down
            return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        }

        return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
    }

    bool IsUpPressed()
    {
        if (AreControlsConfused)
        {
            // Now press left
            return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
        }

        return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
    }

    bool IsDownPressed()
    {
        if (AreControlsConfused)
        {
            // Now press up
            return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
        }

        return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
    }

    float GetMoveSpeed()
    {
        return IsBeingSlowed ? SlowSpeed : MoveSpeed;
    }

    // Update is called once per frame
    void Update()
    {
        if (!Service.Flow.IsGameRunning)
        {
            return;
        }

        if (IsUpPressed())
        {
            if(IsDirectionFreeToMoveIn(Vector2.up))
            {
                transform.position += new Vector3(0, GetMoveSpeed() * GameConfig.GetDeltaTime());
            }

            if (renderer)
            {
                renderer.sprite = upSprite;
            }
        }
        else if (IsDownPressed())
        {
            if (IsDirectionFreeToMoveIn(Vector2.down))
            {
                transform.position += new Vector3(0, -GetMoveSpeed() * GameConfig.GetDeltaTime());
            }

            if (renderer)
            {
                renderer.sprite = downSprite;
            }
        }

        if (IsLeftPressed())
        {
            if (IsDirectionFreeToMoveIn(Vector2.left))
            {
                transform.position += new Vector3(-GetMoveSpeed() * GameConfig.GetDeltaTime(), 0);
            }

            if(renderer)
            {
                renderer.sprite = leftSprite;
            }
        }
        else if (IsRightPressed())
        {
            if (IsDirectionFreeToMoveIn(Vector2.right))
            {
                transform.position += new Vector3(GetMoveSpeed() * GameConfig.GetDeltaTime(), 0);
            }

            if (renderer)
            {
                renderer.sprite = rightSprite;
            }
        }

        if (Input.GetKeyDown(KeyCode.Q) || Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Return))
        {
            InteractNearbyObject();
        }

    }

    void OnDrawGizmos()
    {

        float size = GridManager.TILE_SIZE / 2;

        Gizmos.color = Color.red;

        Vector3 offset = new Vector3(0.5f, 0.5f, 0f);
        Vector2 vec2Pos = transform.position + offset;
        

        Gizmos.DrawLine(transform.position + offset, vec2Pos + new Vector2(0, size));
        Gizmos.DrawLine(transform.position + offset, vec2Pos + new Vector2(0, -size));

        Gizmos.DrawLine(transform.position + offset, vec2Pos + new Vector2(-size, 0));
        Gizmos.DrawLine(transform.position + offset, vec2Pos + new Vector2(size, 0));
    }

    
}
