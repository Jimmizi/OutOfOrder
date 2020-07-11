using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerActor : GridActor
{
    public bool DebugOpenNearbyDoor;

    void Awake()
    {
        Service.Player = this;
    }

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
#if UNITY_EDITOR
        if (DebugOpenNearbyDoor)
        {
            DebugOpenNearbyDoor = false;
            Service.Grid.ToggleNearestDoor(GetGridPosition());
        }
#endif
    }
}
