﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGoal : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Service.Flow.ObjectsToDestroyOnLevelEnd.Add(this.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            Service.Flow.TryToProgressLevel();
        }
    }
}
