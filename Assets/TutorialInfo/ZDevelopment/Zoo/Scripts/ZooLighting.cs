using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

// =====================================================================
// ZOO LIGHTING
// ---------------------------------------------------------------------
// What it does:
//   Press L to cycle through lighting set-ups (for example Day, Sunset,
//   Night) so you can see how your assets look in different conditions.
//   That is one of the main reasons to build an asset zoo.
//
// How to use it:
//   1. Drag the scene's Directional Light into "Sun".
//   2. Drag any lamp lights into "Lamps".
//   3. Fill in the "Presets" list: each preset is one lighting set-up.
// =====================================================================
public class ZooLighting : MonoBehaviour
{
    // =================================================================
    // A Preset holds the settings for ONE lighting set-up.
    // It is a small class written inside this one.
    // [System.Serializable] lets Unity show it in the Inspector.
    // =================================================================
    [System.Serializable]
    public class Preset
    {
        public string name = "Day";
        public Vector3 sunAngles = new Vector3(50f, -30f, 0f);
        public Color sunColour = Color.white;
        public float sunIntensity = 1.3f;
        public Color skyAmbient = new Color(0.6f, 0.65f, 0.75f);
        public Color equatorAmbient = new Color(0.5f, 0.5f, 0.5f);
        public Color groundAmbient = new Color(0.3f, 0.3f, 0.3f);
        public bool lampsOn = false;
    }

    // ---------- Settings you can change in the Inspector ----------

    // The scene's sun (a Directional Light).
    public Light sun;

    // Lamps that switch on or off with each preset.
    public Light[] lamps;

    // The list of lighting set-ups.
    public Preset[] presets;

    // The key that changes to the next preset.
    public Key cycleKey = Key.L;

    // ---------- Private variables ----------

    // Which preset is showing now (0 = the first one).
    private int currentPreset = 0;


    private void Start()
    {
        Apply(0);
    }


    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        if (keyboard[cycleKey].wasPressedThisFrame)
        {
            // Move to the next preset. After the last one, go back to the first.
            int next = currentPreset + 1;
            if (next >= presets.Length)
            {
                next = 0;
            }

            Apply(next);
        }
    }


    // Use preset number "index".
    public void Apply(int index)
    {
        if (presets == null || presets.Length == 0)
        {
            return;
        }

        currentPreset = index;
        Preset preset = presets[index];

        // Move and colour the sun.
        if (sun != null)
        {
            sun.transform.rotation = Quaternion.Euler(preset.sunAngles);
            sun.color = preset.sunColour;
            sun.intensity = preset.sunIntensity;
        }

        // Set the ambient (background) light. These are the Gradient colours
        // in Window > Rendering > Lighting. In code, Gradient is called "Trilight".
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = preset.skyAmbient;
        RenderSettings.ambientEquatorColor = preset.equatorAmbient;
        RenderSettings.ambientGroundColor = preset.groundAmbient;

        // Switch every lamp on or off.
        if (lamps != null)
        {
            for (int i = 0; i < lamps.Length; i++)
            {
                if (lamps[i] != null)
                {
                    lamps[i].enabled = preset.lampsOn;
                }
            }
        }

        // Show the preset's name on screen.
        if (MuseumUI.Instance != null)
        {
            MuseumUI.Instance.ShowTitle("Lighting: " + preset.name + " (press L)");
        }
    }
}
