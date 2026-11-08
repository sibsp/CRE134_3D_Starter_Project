using UnityEngine;

// =====================================================================
// LIGHT SWITCH
// ---------------------------------------------------------------------
// What it does:
//   Turns a group of lights on and off. It can also swap the material
//   on some "bulb" objects, so you can SEE the difference between an
//   emissive glow (a material) and a real light (a Light component).
//
// How to use it:
//   1. Drag your lights into the "Lights" list.
//   2. (Optional) Drag bulb objects into "Bulbs" and set an On and an
//      Off material.
//   3. Call Toggle() from a Trigger Zone or an Interact Prompt.
// =====================================================================
public class LightSwitch : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The lights this switch controls.
    public Light[] lights;

    // Objects whose material changes with the switch (optional).
    public Renderer[] bulbs;

    // The bulb material when the lights are on, and when they are off.
    public Material onMaterial;
    public Material offMaterial;

    // Should the lights be on when the game starts?
    public bool startOn = false;

    // ---------- Private variables ----------

    // Remembers whether the lights are on right now.
    private bool isOn = false;


    private void Start()
    {
        SetOn(startOn);
    }


    // Flip the switch: on becomes off, off becomes on.
    // Public, so you can call it from a Trigger Zone or Interact Prompt.
    public void Toggle()
    {
        if (isOn == true)
        {
            SetOn(false);
        }
        else
        {
            SetOn(true);
        }
    }


    // Turn the lights on (true) or off (false).
    public void SetOn(bool on)
    {
        isOn = on;

        // Switch every light in the list.
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].enabled = on;
            }
        }

        // Change the material on every bulb in the list.
        for (int i = 0; i < bulbs.Length; i++)
        {
            if (bulbs[i] == null)
            {
                continue;
            }

            if (on == true)
            {
                bulbs[i].sharedMaterial = onMaterial;
            }
            else
            {
                bulbs[i].sharedMaterial = offMaterial;
            }
        }
    }
}
