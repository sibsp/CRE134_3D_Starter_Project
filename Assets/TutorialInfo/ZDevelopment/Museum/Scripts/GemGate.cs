using UnityEngine;
using UnityEngine.Events;

// =====================================================================
// GEM GATE
// ---------------------------------------------------------------------
// What it does:
//   Waits until the player has collected enough gems, then runs the
//   actions under "On Unlocked" (once only). For example: show a reward,
//   open a door, play a sound.
//
// How to use it:
//   1. Put this on any object (Lantern Grove uses the "Gems" group).
//   2. Set "Gems Needed".
//   3. Click + under "On Unlocked" and choose what should happen.
// =====================================================================
public class GemGate : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // How many gems the player must collect.
    public int gemsNeeded = 3;

    // What happens when they have enough.
    public UnityEvent onUnlocked = new UnityEvent();

    // ---------- Private variables ----------

    // Becomes true once the gate has opened, so it only happens once.
    private bool unlocked = false;


    private void Start()
    {
        // Ask MuseumUI to tell us every time the gem count changes.
        // "AddListener(CheckGems)" means: whenever onGemsChanged happens,
        // call our CheckGems method below and give it the new number.
        if (MuseumUI.Instance != null)
        {
            MuseumUI.Instance.onGemsChanged.AddListener(CheckGems);
        }
    }


    // Called by MuseumUI each time a gem is collected.
    // "gems" is the new total.
    private void CheckGems(int gems)
    {
        // Already open? Nothing more to do.
        if (unlocked == true)
        {
            return;
        }

        // Not enough gems yet? Keep waiting.
        if (gems < gemsNeeded)
        {
            return;
        }

        unlocked = true;

        // Run every action listed under "On Unlocked" in the Inspector.
        onUnlocked.Invoke();
    }
}
