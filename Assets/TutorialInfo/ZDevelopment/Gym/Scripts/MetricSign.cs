using UnityEngine;
using TMPro;

// =====================================================================
// METRIC SIGN
// ---------------------------------------------------------------------
// What it does:
//   A sign whose numbers come straight from the player, so they are
//   always correct. You write the sign's text with "tokens" in curly
//   brackets, and each token is swapped for the real number.
//
//   Example template:   Max step: {step} m
//   Shows on the sign:  Max step: 0.25 m
//
//   Tokens you can use:
//     {walk} {sprint} {jump} {step} {slope} {height} {width} {eye}
//     {air} {jumpWalk} {jumpSprint}
//
// How to use it:
//   Add it to an object with a TextMeshPro text and type the template.
//
// [ExecuteAlways] means this script also runs in Edit mode (not just
// when you press Play), so you can see the real numbers while building.
// =====================================================================
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class MetricSign : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The text, with tokens. [TextArea] gives a bigger typing box.
    [TextArea(2, 8)]
    public string template = "Jump height: {jump} m";

    // ---------- Private variables ----------

    // The text component on this object.
    private TMP_Text label;

    // Counts down so we only refresh twice a second, not every frame.
    private float refreshTimer = 0f;


    // OnEnable runs whenever the object is switched on (including when the scene loads).
    private void OnEnable()
    {
        label = GetComponent<TMP_Text>();
        Refresh();
    }


    // OnValidate runs in the Editor whenever you change a value in the Inspector.
    private void OnValidate()
    {
        label = GetComponent<TMP_Text>();
        Refresh();
    }


    private void Update()
    {
        // unscaledDeltaTime keeps counting even if the game is paused.
        refreshTimer = refreshTimer - Time.unscaledDeltaTime;

        if (refreshTimer <= 0f)
        {
            refreshTimer = 0.5f;
            Refresh();
        }
    }


    // Read the player's numbers and write them into the sign.
    public void Refresh()
    {
        if (label == null)
        {
            return;
        }

        GymMetrics numbers = GymMetrics.ReadFromPlayer();

        // Start with the template, then swap each token for its number.
        // ToString("0.##") means "show up to 2 decimal places".
        string text = template;
        text = text.Replace("{walk}", numbers.walkSpeed.ToString("0.#"));
        text = text.Replace("{sprint}", numbers.sprintSpeed.ToString("0.#"));
        text = text.Replace("{jump}", numbers.jumpHeight.ToString("0.##"));
        text = text.Replace("{step}", numbers.stepOffset.ToString("0.##"));
        text = text.Replace("{slope}", numbers.slopeLimit.ToString("0"));
        text = text.Replace("{height}", numbers.height.ToString("0.##"));
        text = text.Replace("{width}", numbers.width.ToString("0.##"));
        text = text.Replace("{eye}", numbers.eyeHeight.ToString("0.##"));
        text = text.Replace("{air}", numbers.GetAirTime().ToString("0.##"));
        text = text.Replace("{jumpWalk}", numbers.GetJumpDistanceWalking().ToString("0.#"));
        text = text.Replace("{jumpSprint}", numbers.GetJumpDistanceSprinting().ToString("0.#"));

        label.text = text;
    }
}
