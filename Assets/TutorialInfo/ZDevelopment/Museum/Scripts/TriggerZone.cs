using UnityEngine;
using UnityEngine.Events;

// =====================================================================
// TRIGGER ZONE
// ---------------------------------------------------------------------
// What it does:
//   Notices when the player walks into (or out of) an invisible area,
//   then runs whatever actions you have set up in the Inspector.
//
// How to use it:
//   1. Add this script to an object that has a Collider (e.g. a Box Collider).
//   2. Tick "Is Trigger" on the collider (this script does it for you).
//   3. In the Inspector, click + under "On Player Enter" and choose
//      what should happen, e.g. MuseumUI > ShowTitle.
//   No code needed!
// =====================================================================
[RequireComponent(typeof(Collider))]
public class TriggerZone : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // Only objects with this tag will set off the trigger.
    [Tooltip("Only react to objects with this tag.")]
    public string playerTag = "Player";

    // If ticked, the "enter" actions only happen the first time.
    [Tooltip("Tick to only fire the first time the player enters.")]
    public bool onlyOnce = false;

    // A UnityEvent is a list of actions you fill in using the Inspector.
    public UnityEvent onPlayerEnter = new UnityEvent();
    public UnityEvent onPlayerExit = new UnityEvent();

    // ---------- Private variables (only this script can use these) ----------

    // Remembers whether the enter actions have already happened.
    private bool hasFired = false;


    // Reset() is called by Unity when you first add this script to an object.
    // We use it to tick "Is Trigger" on the collider for you.
    private void Reset()
    {
        Collider myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;
    }


    // Unity calls OnTriggerEnter when another collider walks INTO this trigger.
    // "other" is the collider that came in.
    private void OnTriggerEnter(Collider other)
    {
        // Ignore anything that is not the player (animals, crates...).
        if (other.CompareTag(playerTag) == false)
        {
            return;
        }

        // If we only want this to happen once, and it already has, stop here.
        if (onlyOnce == true && hasFired == true)
        {
            return;
        }

        hasFired = true;

        // Run every action listed under "On Player Enter" in the Inspector.
        onPlayerEnter.Invoke();
    }


    // Unity calls OnTriggerExit when another collider walks OUT of this trigger.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag) == false)
        {
            return;
        }

        // Run every action listed under "On Player Exit" in the Inspector.
        onPlayerExit.Invoke();
    }
}
