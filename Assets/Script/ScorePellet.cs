using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScorePellet : MonoBehaviour
{
    public static List<ScorePellet> PelletList = new List<ScorePellet>();

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

    void Start()
    {
        PelletList.Add(this);
    }

    void OnDestroy()
    {
        PelletList.Remove(this);
    }

    // Update is called once per frame
    void Update()
    {
        
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
