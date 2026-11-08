using System.Collections;
using UnityEngine;

// =====================================================================
// LIGHTNING FLASH
// ---------------------------------------------------------------------
// What it does:
//   Every few seconds, makes a light flash brightly 1 to 3 times, like
//   lightning. It can also play a thunder sound shortly afterwards.
//
// How to use it:
//   Add it to an object with a Light (a Directional Light works well).
//   Optionally drag an Audio Source with a thunder clip into "Thunder".
//
// New idea: a COROUTINE
//   Start() below returns "IEnumerator". That turns it into a coroutine:
//   a method that can pause part-way through. "yield return new
//   WaitForSeconds(2f)" means "stop here, wait 2 seconds, then carry on
//   from this line". That makes timed sequences much easier to write.
// =====================================================================
[RequireComponent(typeof(Light))]
public class LightningFlash : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // How bright a flash is.
    public float flashIntensity = 4f;

    // The shortest and longest wait between storms of flashes, in seconds.
    public float minWait = 5f;
    public float maxWait = 12f;

    // Optional: a thunder sound to play after the flashes.
    public AudioSource thunder;

    // ---------- Private variables ----------

    // The Light on this object.
    private Light myLight;

    // The light's normal brightness, to go back to after each flash.
    private float normalIntensity;


    // Start is a coroutine here (see the note at the top).
    private IEnumerator Start()
    {
        myLight = GetComponent<Light>();
        normalIntensity = myLight.intensity;

        // Loop forever (the loop ends when the scene ends).
        while (true)
        {
            // Wait a random time before the next storm.
            float wait = Random.Range(minWait, maxWait);
            yield return new WaitForSeconds(wait);

            // Random.Range with whole numbers never returns the top number,
            // so this gives 1, 2 or 3 flashes.
            int numberOfFlashes = Random.Range(1, 4);

            for (int i = 0; i < numberOfFlashes; i++)
            {
                // Flash on (slightly random brightness)...
                myLight.intensity = flashIntensity * Random.Range(0.7f, 1f);
                yield return new WaitForSeconds(Random.Range(0.04f, 0.12f));

                // ...and back off.
                myLight.intensity = normalIntensity;
                yield return new WaitForSeconds(Random.Range(0.05f, 0.2f));
            }

            // Thunder arrives a moment after the lightning (light travels faster than sound!).
            if (thunder != null && thunder.clip != null)
            {
                yield return new WaitForSeconds(Random.Range(0.5f, 1.5f));
                thunder.Play();
            }
        }
    }
}
