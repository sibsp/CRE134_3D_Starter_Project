using UnityEngine;

// =====================================================================
// COLLECTIBLE  (the glowing gems)
// ---------------------------------------------------------------------
// What it does:
//   Spins and bobs up and down so it catches the eye. When the player
//   walks into it, it adds one gem to the counter, plays a sparkle
//   effect (if you gave it one) and disappears.
//
// How to use it:
//   Add it to an object with a Collider set to "Is Trigger".
//   The scene needs a MuseumUI to count the gems.
// =====================================================================
[RequireComponent(typeof(Collider))]
public class Collectible : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // How fast it spins, in degrees per second.
    public float spinSpeed = 90f;

    // How far it moves up and down, in metres.
    public float bobHeight = 0.15f;

    // How fast it moves up and down.
    public float bobSpeed = 2f;

    // Optional: a particle effect to play when it is collected.
    public ParticleSystem collectEffect;

    // ---------- Private variables ----------

    // Where the gem started, so it bobs around that point.
    private Vector3 startPosition;


    // Start runs once, when the game begins.
    private void Start()
    {
        startPosition = transform.position;
    }


    // Update runs every frame: spin a little, and bob up or down a little.
    private void Update()
    {
        // Spin around the world's up axis.
        // Multiplying by Time.deltaTime makes it "degrees per second", not per frame.
        float degreesThisFrame = spinSpeed * Time.deltaTime;
        transform.Rotate(0f, degreesThisFrame, 0f, Space.World);

        // Mathf.Sin smoothly swings between -1 and 1 as time goes on.
        // Multiplying by bobHeight turns that into a small distance up or down.
        float wave = Mathf.Sin(Time.time * bobSpeed);
        float heightOffset = wave * bobHeight;
        transform.position = startPosition + new Vector3(0f, heightOffset, 0f);
    }


    // The player walked into the gem: collect it.
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") == false)
        {
            return;
        }

        // Tell the on-screen UI to add one gem to the counter.
        if (MuseumUI.Instance != null)
        {
            MuseumUI.Instance.AddGem();
        }

        // Play the sparkle. We un-parent it first, otherwise it would be
        // hidden along with the gem on the next line.
        if (collectEffect != null)
        {
            collectEffect.transform.SetParent(null);
            collectEffect.Play();
        }

        // Switch the whole gem off so it disappears.
        gameObject.SetActive(false);
    }
}
