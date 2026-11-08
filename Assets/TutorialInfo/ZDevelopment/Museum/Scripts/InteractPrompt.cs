using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

// =====================================================================
// INTERACT PROMPT  ("Press E to ...")
// ---------------------------------------------------------------------
// What it does:
//   While the player stands inside this trigger, a prompt such as
//   "Press E to read the signpost" appears on screen. If they press the
//   key, the actions under "On Interact" run.
//
// How to use it:
//   1. Add this script to an object with a Collider set to "Is Trigger"
//      (a Sphere Collider with radius 2 works well).
//   2. Type the prompt text in the Inspector.
//   3. Click + under "On Interact" and choose what should happen.
//   The scene also needs a MuseumUI (the on-screen UI) to show the prompt.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class InteractPrompt : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The message shown in the middle of the screen.
    public string promptText = "Press E to interact";

    // Which keyboard key the player presses.
    public Key key = Key.E;

    // The actions that happen when the key is pressed.
    public UnityEvent onInteract = new UnityEvent();

    // ---------- Private variables ----------

    // True while the player is standing inside the trigger.
    private bool playerInside = false;


    // The player walked into the trigger: remember that, and show the prompt.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        playerInside = true;

        // MuseumUI.Instance is the one on-screen UI in the scene (see MuseumUI.cs).
        if (MuseumUI.Instance != null)
        {
            MuseumUI.Instance.ShowPrompt(promptText);
        }
    }


    // The player walked out of the trigger: forget it, and hide the prompt.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        playerInside = false;

        if (MuseumUI.Instance != null)
        {
            MuseumUI.Instance.HidePrompt();
        }
    }


    // Update runs every frame. We check whether the key was pressed.
    private void Update()
    {
        // If the player isn't standing here, there is nothing to check.
        if (playerInside == false)
        {
            return;
        }

        // Keyboard.current is the keyboard (it can be missing, e.g. on a phone).
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // wasPressedThisFrame is only true on the frame the key goes down,
        // so holding the key does not repeat the action.
        if (keyboard[key].wasPressedThisFrame)
        {
            onInteract.Invoke();
        }
    }
}
