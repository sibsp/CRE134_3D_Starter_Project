using UnityEngine;

// =====================================================================
// FOG ZONE
// ---------------------------------------------------------------------
// What it does:
//   While the player is inside this box, fog is switched on. The deeper
//   they walk into the box (along its blue Z arrow), the thicker the fog.
//   When they leave, the scene's original fog settings come back.
//
// How to use it:
//   Add it to an object with a Box Collider set to "Is Trigger".
//   Point the blue Z arrow the way the player walks in.
// =====================================================================
[RequireComponent(typeof(BoxCollider))]
public class FogZone : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    public Color fogColour = new Color(0.75f, 0.78f, 0.82f);
    public FogMode mode = FogMode.ExponentialSquared;

    // Fog thickness at the entrance and at the far end of the box.
    [Range(0f, 0.3f)]
    public float densityAtStart = 0.01f;

    [Range(0f, 0.3f)]
    public float densityAtEnd = 0.2f;

    // ---------- Private variables ----------

    // The player, while they are inside the zone (empty when outside).
    private Transform player;

    // The scene's fog settings from before the player came in,
    // so we can put them back when they leave.
    private bool savedFogOn;
    private Color savedColour;
    private FogMode savedMode;
    private float savedDensity;


    // The player walked in: remember the old fog settings, then turn our fog on.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        player = other.transform;

        // Save the current settings.
        savedFogOn = RenderSettings.fog;
        savedColour = RenderSettings.fogColor;
        savedMode = RenderSettings.fogMode;
        savedDensity = RenderSettings.fogDensity;

        // RenderSettings holds the values from the Lighting window.
        RenderSettings.fog = true;
        RenderSettings.fogColor = fogColour;
        RenderSettings.fogMode = mode;
    }


    // The player walked out: put the old fog settings back.
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        player = null;

        RenderSettings.fog = savedFogOn;
        RenderSettings.fogColor = savedColour;
        RenderSettings.fogMode = savedMode;
        RenderSettings.fogDensity = savedDensity;
    }


    // Every frame while the player is inside, set the fog thickness
    // depending on how far into the box they are.
    private void Update()
    {
        if (player == null)
        {
            return;
        }

        BoxCollider box = GetComponent<BoxCollider>();

        // Where is the player compared with the middle of the box?
        // InverseTransformPoint gives a position in the box's own directions.
        Vector3 playerInBox = transform.InverseTransformPoint(player.position) - box.center;

        // How far along the box are they? 0 = entrance, 1 = far end.
        float halfLength = box.size.z / 2f;
        float howFar = Mathf.InverseLerp(-halfLength, halfLength, playerInBox.z);

        // Lerp picks a value between the start and end density.
        RenderSettings.fogDensity = Mathf.Lerp(densityAtStart, densityAtEnd, howFar);
    }


    // =================================================================
    // Call these from a Trigger Zone to compare the three fog types.
    // =================================================================

    public void UseLinear()
    {
        mode = FogMode.Linear;
        RenderSettings.fogMode = mode;
        RenderSettings.fogStartDistance = 1f;
        RenderSettings.fogEndDistance = 12f;
    }


    public void UseExponential()
    {
        mode = FogMode.Exponential;
        RenderSettings.fogMode = mode;
    }


    public void UseExponentialSquared()
    {
        mode = FogMode.ExponentialSquared;
        RenderSettings.fogMode = mode;
    }
}
