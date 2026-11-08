using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using TMPro;

// =====================================================================
// MUSEUM UI  (the on-screen text and counters)
// ---------------------------------------------------------------------
// What it does:
//   Controls everything drawn flat on the screen:
//     - the place title at the top (fades in, then out after a few seconds)
//     - the info panel at the bottom
//     - the "Press E" prompt in the middle
//     - the "places found" and "gems" counters in the top left
//
// How to use it:
//   Put it on the Canvas and drag the UI pieces into its slots.
//   Other scripts and Trigger Zones then call its PUBLIC methods,
//   such as ShowTitle("Deer Pond") or AddGem().
//
// There should only be ONE MuseumUI in a scene. Any script can reach it
// by writing MuseumUI.Instance (see the note on "Instance" below).
// =====================================================================
public class MuseumUI : MonoBehaviour
{
    // ---------- The one MuseumUI in the scene ----------

    // "static" means this variable belongs to the MuseumUI CLASS, not to
    // one particular object. So any script can write MuseumUI.Instance
    // to find the UI without needing a slot in the Inspector.
    // We set it in Awake() below.
    public static MuseumUI Instance;

    // ---------- Settings and UI pieces (drag these in the Inspector) ----------

    [Header("Place title (top of screen)")]
    public CanvasGroup titleGroup;
    public TMP_Text titleText;
    public float titleShowTime = 3f;

    [Header("Info panel (bottom of screen)")]
    public CanvasGroup infoGroup;
    public TMP_Text infoText;

    [Header("Interaction prompt (centre)")]
    public CanvasGroup promptGroup;
    public TMP_Text promptText;

    [Header("Counters (top left)")]
    public TMP_Text wingsText;
    public TMP_Text gemsText;
    public int totalWings = 8;

    [Tooltip("Text shown before the visited count, e.g. 'Wings visited' or 'Places found'")]
    public string visitedLabel = "Wings visited";

    [Header("Fade speed")]
    public float fadeSpeed = 4f;

    // Other scripts can "listen" to this to hear when the gem count changes.
    // It sends the new number of gems (an int). GemGate uses it.
    public UnityEvent<int> onGemsChanged = new UnityEvent<int>();

    // ---------- Private variables ----------

    // The names of the places the player has already found.
    private List<string> visitedPlaces = new List<string>();

    // How many gems have been collected.
    private int gems = 0;

    // Counts down how long the title stays on screen.
    private float titleTimer = 0f;

    // Whether the info panel and prompt should be showing.
    private bool infoVisible = false;
    private bool promptVisible = false;


    // Awake runs before Start, as soon as the object is created.
    // We fill in Instance here so it is ready before other scripts look for it.
    private void Awake()
    {
        Instance = this;

        // Start with everything hidden.
        SetAlpha(titleGroup, 0f);
        SetAlpha(infoGroup, 0f);
        SetAlpha(promptGroup, 0f);

        UpdateCounters();
    }


    // Every frame, fade each piece of UI towards visible or hidden.
    private void Update()
    {
        titleTimer = titleTimer - Time.deltaTime;

        bool showTitle = false;
        if (titleTimer > 0f)
        {
            showTitle = true;
        }

        Fade(titleGroup, showTitle);
        Fade(infoGroup, infoVisible);
        Fade(promptGroup, promptVisible);
    }


    // =================================================================
    // PUBLIC METHODS
    // Because these are public and take simple values (text or nothing),
    // they appear in the drop-down list of a Trigger Zone's events.
    // =================================================================

    // Show a title at the top of the screen for a few seconds.
    public void ShowTitle(string title)
    {
        if (titleText != null)
        {
            titleText.text = title;
        }
        titleTimer = titleShowTime;
    }


    // Show some information in the panel at the bottom of the screen.
    public void ShowInfo(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
        infoVisible = true;
    }


    // Hide the information panel.
    public void HideInfo()
    {
        infoVisible = false;
    }


    // Show a prompt in the middle of the screen, e.g. "Press E to ...".
    public void ShowPrompt(string text)
    {
        if (promptText != null)
        {
            promptText.text = text;
        }
        promptVisible = true;
    }


    // Hide the prompt.
    public void HidePrompt()
    {
        promptVisible = false;
    }


    // Count a place as found. Each place name only counts once,
    // so walking back into the same area does not add to the total.
    public void VisitWing(string wingName)
    {
        if (visitedPlaces.Contains(wingName))
        {
            return;
        }

        visitedPlaces.Add(wingName);
        UpdateCounters();
    }


    // Add one gem, update the counter, and tell anyone listening.
    public void AddGem()
    {
        gems = gems + 1;
        UpdateCounters();

        // Send the new total to every listener (for example GemGate).
        onGemsChanged.Invoke(gems);
    }


    // Lets other scripts ask how many gems have been collected.
    public int GetGemCount()
    {
        return gems;
    }


    // =================================================================
    // PRIVATE METHODS (only used inside this script)
    // =================================================================

    // Write the latest numbers into the counter text.
    private void UpdateCounters()
    {
        if (wingsText != null)
        {
            wingsText.text = visitedLabel + ": " + visitedPlaces.Count + " / " + totalWings;
        }

        if (gemsText != null)
        {
            gemsText.text = "Gems: " + gems;
        }
    }


    // Move a group's transparency (alpha) a little towards fully visible (1)
    // or fully hidden (0). Doing a little each frame makes a smooth fade.
    private void Fade(CanvasGroup group, bool show)
    {
        if (group == null)
        {
            return;
        }

        float target = 0f;
        if (show == true)
        {
            target = 1f;
        }

        group.alpha = Mathf.MoveTowards(group.alpha, target, fadeSpeed * Time.deltaTime);
    }


    // Set a group's transparency straight away (no fade).
    private void SetAlpha(CanvasGroup group, float alpha)
    {
        if (group != null)
        {
            group.alpha = alpha;
        }
    }
}
