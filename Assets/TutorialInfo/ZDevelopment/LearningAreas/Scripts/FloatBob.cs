using UnityEngine;

// =====================================================================
// FLOAT BOB
// ---------------------------------------------------------------------
// What it does:
//   Gently bobs an object up and down, like a floating candle, a ghost
//   or a spider on a thread.
//
// How to use it:
//   Add it to any object. Change Height and Speed in the Inspector.
// =====================================================================
public class FloatBob : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // How far it moves up and down, in metres.
    public float height = 0.15f;

    // How fast it bobs.
    public float speed = 1f;

    // ---------- Private variables ----------

    // Where the object started.
    private Vector3 startPosition;

    // A random head start, so several bobbing objects don't move in sync.
    private float randomOffset;


    private void Start()
    {
        // localPosition = position relative to the parent object.
        startPosition = transform.localPosition;

        // Random.value is a random number between 0 and 1.
        randomOffset = Random.value * 10f;
    }


    private void Update()
    {
        // Mathf.Sin smoothly swings between -1 and 1 as time goes on.
        float wave = Mathf.Sin((Time.time + randomOffset) * speed);

        float heightOffset = wave * height;
        transform.localPosition = startPosition + new Vector3(0f, heightOffset, 0f);
    }
}
