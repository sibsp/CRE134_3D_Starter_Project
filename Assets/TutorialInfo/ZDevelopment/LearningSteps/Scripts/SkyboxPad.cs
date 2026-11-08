using UnityEngine;
using UnityEngine.Rendering;

// =====================================================================
// SKYBOX PAD  (Step 10 - Skyboxes)
// ---------------------------------------------------------------------
// What it does:
//   When the player steps on this pad, it changes the whole "time of day":
//     - the skybox (the sky material)
//     - the sun (its direction, colour and brightness)
//     - the ambient light (the soft light that fills in shadows)
//     - the fog colour
//   A new sky only looks right if the lighting matches it, which is
//   why we change all four together.
//   The pad you are standing on glows; the others go dim.
//
// How to use it:
//   1. Add it to an object with a Collider set to "Is Trigger".
//   2. Drag a skybox material into "Skybox" and the scene's
//      Directional Light into "Sun".
//   3. Choose the sun, ambient and fog settings that suit that sky.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class SkyboxPad : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    [Header("Sky")]
    [Tooltip("Any material that uses a Skybox shader (Procedural, Panoramic, 6 Sided or Cubemap).")]
    public Material skybox;

    [Header("Sun (Directional Light)")]
    public Light sun;
    public Vector3 sunRotation = new Vector3(50f, 325f, 0f);
    public Color sunColour = Color.white;

    [Range(0f, 3f)]
    public float sunIntensity = 1f;

    [Header("Ambient Light (Trilight)")]
    public Color ambientSky = Color.grey;
    public Color ambientEquator = Color.grey;
    public Color ambientGround = Color.black;

    [Header("Fog")]
    [Tooltip("Fog looks best when it matches the colour of the sky at the horizon.")]
    public Color fogColour = Color.grey;

    [Header("Pad Highlight")]
    public Renderer padRenderer;
    public Color idleGlow = new Color(0.15f, 0.15f, 0.15f);
    public Color activeGlow = new Color(1.6f, 1.3f, 0.7f);

    // ---------- Private variables ----------

    // Which pad is lit up at the moment.
    // "static" means there is only ONE of this variable, shared by every
    // SkyboxPad in the scene. So when a new pad lights up, it can find the
    // old one and dim it.
    private static SkyboxPad activePad;


    // Reset() runs when you first add this script. It ticks "Is Trigger" for you.
    private void Reset()
    {
        Collider myCollider = GetComponent<Collider>();
        myCollider.isTrigger = true;
    }


    private void Start()
    {
        SetGlow(false);

        // If this pad's sky is the one the scene starts with, light it up.
        if (RenderSettings.skybox == skybox)
        {
            activePad = this;
            SetGlow(true);
        }
    }


    // The player stepped on the pad.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Apply();
        }
    }


    // Change the sky, sun, ambient light and fog to this pad's settings.
    public void Apply()
    {
        // 1. The sky.
        // RenderSettings holds the values from Window > Rendering > Lighting.
        // This is the "Skybox Material" slot.
        RenderSettings.skybox = skybox;

        // 2. The sun.
        if (sun != null)
        {
            // Quaternion.Euler turns the X, Y, Z angles (like in the Inspector)
            // into a rotation.
            sun.transform.rotation = Quaternion.Euler(sunRotation);
            sun.color = sunColour;
            sun.intensity = sunIntensity;
        }

        // 3. The ambient light: Gradient mode, which code calls "Trilight".
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSky;
        RenderSettings.ambientEquatorColor = ambientEquator;
        RenderSettings.ambientGroundColor = ambientGround;

        // 4. The fog.
        RenderSettings.fogColor = fogColour;

        // Ask Unity to refresh lighting that comes from the sky (such as reflections).
        DynamicGI.UpdateEnvironment();

        // Dim the pad that was lit before, and light up this one.
        if (activePad != null && activePad != this)
        {
            activePad.SetGlow(false);
        }

        activePad = this;
        SetGlow(true);
    }


    // Make the pad glow brightly (true) or dimly (false).
    private void SetGlow(bool on)
    {
        if (padRenderer == null)
        {
            return;
        }

        // .material gives this pad its OWN copy of the material, so changing
        // the glow here doesn't change the other pads (see Step 02).
        Material padMaterial = padRenderer.material;

        if (on == true)
        {
            padMaterial.SetColor("_EmissionColor", activeGlow);
        }
        else
        {
            padMaterial.SetColor("_EmissionColor", idleGlow);
        }
    }
}
