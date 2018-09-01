using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepairField : MonoBehaviour {

    public float repairPerSecond = 1f;

    private void OnTriggerStay2D(Collider2D other)
    {
        var repairable = other.GetComponent<Repairable>();
        if (repairable != null)
        {
            repairable.repair(Time.smoothDeltaTime * repairPerSecond);
        }
    }
}
