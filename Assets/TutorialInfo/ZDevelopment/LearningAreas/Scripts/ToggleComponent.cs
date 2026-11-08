using UnityEngine;

// =====================================================================
// TOGGLE COMPONENT
// ---------------------------------------------------------------------
// What it does:
//   Switches one component (a Volume, a Light, a script...) on and off.
//
// How to use it:
//   1. Drag the component you want to switch into "Target".
//   2. Call Toggle(), TurnOn() or TurnOff() from a Trigger Zone
//      or an Interact Prompt.
// =====================================================================
public class ToggleComponent : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The component to switch on and off.
    // "Behaviour" means any component that has an "enabled" tick box.
    public Behaviour target;


    // On becomes off, off becomes on.
    public void Toggle()
    {
        if (target == null)
        {
            return;
        }

        if (target.enabled == true)
        {
            target.enabled = false;
        }
        else
        {
            target.enabled = true;
        }
    }


    // Switch it on.
    public void TurnOn()
    {
        if (target != null)
        {
            target.enabled = true;
        }
    }


    // Switch it off.
    public void TurnOff()
    {
        if (target != null)
        {
            target.enabled = false;
        }
    }
}
