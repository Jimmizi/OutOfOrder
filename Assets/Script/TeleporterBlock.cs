using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterBlock : MonoBehaviour
{
    public TeleporterBlock LinkedTeleporter;

    public bool CanTeleportPlayer;

    public Vector2 TeleportOffset;

    public Vector3 GetTeleportLocation()
    {
        return transform.localPosition + new Vector3(TeleportOffset.x, TeleportOffset.y, 0f);
    }

    // Start is called before the first frame update
    void Start()
    {
        CanTeleportPlayer = true;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.tag != "Player" || !CanTeleportPlayer)
        {
            return;
        }

        LinkedTeleporter.CanTeleportPlayer = false;
        other.transform.position = LinkedTeleporter.GetTeleportLocation();

        Camera.main.GetComponent<CameraFollow>()?.SetTeleportedSpeedOverride();
        GridActor.TellEnemiesToLosePlayer();
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.tag != "Player")
        {
            return;
        }

        CanTeleportPlayer = true;
    }


    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, transform.position + new Vector3(TeleportOffset.x, TeleportOffset.y, 0f));
        Gizmos.DrawSphere(transform.position + new Vector3(TeleportOffset.x, TeleportOffset.y, 0f), 0.2f);
    }
}
