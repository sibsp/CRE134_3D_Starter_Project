using UnityEngine;

// =====================================================================
// TELEPORT PAD
// ---------------------------------------------------------------------
// What it does:
//   Step on this pad to be teleported to a gym station.
//
// How to use it:
//   Add it to an object with a Collider set to "Is Trigger", and set
//   "Station Index" to the station number in the GymTeleporter's list.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class TeleportPad : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // Which station to send the player to (0 = the first in the list).
    public int stationIndex = 0;


    // Reset() runs when you first add this script. It ticks "Is Trigger" for you.
    private void Reset()
    {
        Collider myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;
    }


    // The player stepped on the pad: teleport them.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        if (GymTeleporter.Instance != null)
        {
            GymTeleporter.Instance.Teleport(stationIndex);
        }
    }
}
