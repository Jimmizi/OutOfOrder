using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorePellet : GridActor
{
    public static List<ScorePellet> PelletList = new List<ScorePellet>();

    private Path currentPath;

    public static int GetNumberOfPellets()
    {
        int numPellets = 0;

        foreach (var actor in PelletList)
        {
            numPellets++;
        }

        return numPellets;
    }

    public Vector2Int GetGridPosition()
    {
        return new Vector2Int((int)Mathf.Round(transform.position.x), (int)Mathf.Round(transform.position.y));
    }

    public override void Start()
    {
        PelletList.Add(this);
    }

    public override void OnDestroy()
    {
        PelletList.Remove(this);
    }

    // Update is called once per frame
    public override void Update()
    {
        if (currentPath == null)
        {
            var currentPos = GetGridPosition();
            var targetPosition = Service.Grid.GetPositionNearby(currentPos);
            currentPath = Service.Grid.GetPath(currentPos, targetPosition);
            currentPath.UpdateProgression(GetGridPosition());
        }
        else
        {
            if (TaskMoveAlongPath(ref currentPath))
            {
                currentPath = null;

            }
        }
    }

    void OnTriggerEnter2D(Collider2D col)
    {
        if (col.transform.tag == "Player")
        {
            Service.Flow.AddScore();
            Destroy(this.gameObject);
        }
    }
}
