using UnityEngine;

// =====================================================================
// LIGHT FLICKER
// ---------------------------------------------------------------------
// What it does:
//   Makes a light flicker like a flame by changing its brightness
//   up and down smoothly but unpredictably.
//
// How to use it:
//   Add it to an object that has a Light. Set the lowest and highest
//   brightness, and how fast it flickers.
// =====================================================================
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    // The dimmest and brightest the light will go.
    public float minIntensity = 1.5f;
    public float maxIntensity = 3f;

    // How quickly it flickers.
    [Range(1f, 30f)]
    public float speed = 10f;

    // ---------- Private variables ----------

    // The Light component on this object.
    private Light myLight;

    // A random starting point, so two flickering lights don't match.
    private float randomSeed;


    // Awake runs as soon as the object is created (before Start).
    private void Awake()
    {
        myLight = GetComponent<Light>();
        randomSeed = Random.value * 100f;
    }


    private void Update()
    {
        // Perlin noise is "smooth randomness": a value between 0 and 1
        // that drifts up and down instead of jumping about.
        float noise = Mathf.PerlinNoise(randomSeed, Time.time * speed);

        // Lerp picks a value between min and max: noise 0 = min, noise 1 = max.
        myLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, noise);
    }
}
