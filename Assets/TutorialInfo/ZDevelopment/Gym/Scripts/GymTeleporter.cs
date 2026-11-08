using UnityEngine;
using UnityEngine.InputSystem;

// =====================================================================
// GYM TELEPORTER
// ---------------------------------------------------------------------
// What it does:
//   Press a number key (0-9) to jump straight to a gym station.
//   Teleport Pads (TeleportPad.cs) use it too.
//
// How to use it:
//   1. Make an empty GameObject for each station. Its blue Z arrow is
//      the direction the player will face when they arrive.
//   2. Drag the stations into the "Stations" list, in order.
//   3. Type a name for each one in "Station Names" (shown on screen).
//   Station 0 is key 0, station 1 is key 1, and so on.
// =====================================================================
public class GymTeleporter : MonoBehaviour
{
    // ---------- The one teleporter in the scene ----------

    // "static" means this belongs to the class, so any script can write
    // GymTeleporter.Instance to find it. We set it in Awake().
    public static GymTeleporter Instance;

    // ---------- Settings you can change in the Inspector ----------

    // Where each station is.
    public Transform[] stations;

    // The name of each station, shown at the top of the screen.
    public string[] stationNames;

    // ---------- Private variables ----------

    // The player, and its Character Controller.
    private Transform player;
    private CharacterController controller;

    // The number keys along the top of the keyboard, in order.
    // numberKeys[0] is the 0 key, numberKeys[1] is the 1 key, and so on.
    private Key[] numberKeys = new Key[]
    {
        Key.Digit0, Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4,
        Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9
    };


    private void Awake()
    {
        Instance = this;
    }


    private void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            controller = playerObject.GetComponent<CharacterController>();
        }
    }


    // Every frame, check whether a number key was pressed.
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        for (int i = 0; i < stations.Length; i++)
        {
            // There are only 10 number keys.
            if (i >= numberKeys.Length)
            {
                break;
            }

            if (keyboard[numberKeys[i]].wasPressedThisFrame)
            {
                Teleport(i);
            }
        }
    }


    // Move the player to station number "index".
    public void Teleport(int index)
    {
        // Check everything we need is there before we try.
        if (player == null)
        {
            return;
        }

        if (index < 0 || index >= stations.Length)
        {
            return;
        }

        if (stations[index] == null)
        {
            return;
        }

        // A Character Controller must be switched off before you can move
        // the player directly, otherwise it puts them straight back.
        if (controller != null)
        {
            controller.enabled = false;
        }

        // Face the same way as the station, but only turning left/right (Y).
        float facing = stations[index].eulerAngles.y;
        player.SetPositionAndRotation(stations[index].position, Quaternion.Euler(0f, facing, 0f));

        if (controller != null)
        {
            controller.enabled = true;
        }

        // Show the station's name on screen.
        if (MuseumUI.Instance != null && stationNames != null && index < stationNames.Length)
        {
            MuseumUI.Instance.ShowTitle(stationNames[index]);
        }
    }
}
