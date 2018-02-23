using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairBase : MonoBehaviour
{

    public float repairPerSecond = 1f;
    private void OnTriggerStay2D(Collider2D other)
    {
        if (other.tag == "Player")
        {
            var player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                player.repair(Time.smoothDeltaTime * repairPerSecond);
            }
        }
    }
}
