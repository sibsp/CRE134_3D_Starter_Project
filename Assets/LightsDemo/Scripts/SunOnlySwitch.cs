using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

// =====================================================================
// SUN ONLY SWITCH
// ---------------------------------------------------------------------
// Turns off everything that gives off light EXCEPT the Directional Light
// (the sun), and turns it all back on again. Works in the Editor
// without pressing Play.
//
// How to use it:
//   Put it on any GameObject (e.g. an empty called "Sun Only Switch").
//   The Inspector shows two buttons:
//     SUN ONLY        everything except the sun goes off
//     ALL LIGHTS ON   it all comes back on
//   In Play mode you can also press K.
//
//   "Also turn off glowing materials" and "Also turn off particles"
//   choose what else goes off with the lights when you press SUN ONLY.
//
// What it switches off:
//   Lights            every Light component that isn't Directional
//   Glowing materials anything using a material with Emission
//                     (bulbs, candle flames, embers, the star)
//   Particles         particle effects such as the fireplace flames
//
// The glow is turned off on each OBJECT, not in the material file, so
// other scenes that use the same materials (e.g. the Christmas Cottage)
// are not changed.
// =====================================================================
[ExecuteAlways]
public class SunOnlySwitch : MonoBehaviour
{
    [Tooltip("When you press SUN ONLY, also turn off the glow on emissive materials (bulbs, flames, embers, star).")]
    [FormerlySerializedAs("includeGlowingMaterials")]
    public bool alsoTurnOffGlow = true;

    [Tooltip("When you press SUN ONLY, also hide particle effects (e.g. the fireplace flames).")]
    [FormerlySerializedAs("includeParticles")]
    public bool alsoTurnOffParticles = true;

    [Tooltip("Press this in Play mode to flip between Sun Only and All Lights On.")]
    public Key toggleKey = Key.K;

    // What this script turned off, so it can turn the same things back on.
    [FormerlySerializedAs("switchedOff")]
    [SerializeField, HideInInspector] private List<Light> switchedOffLights = new List<Light>();
    [SerializeField, HideInInspector] private List<Renderer> dimmedRenderers = new List<Renderer>();
    [SerializeField, HideInInspector] private List<ParticleSystemRenderer> hiddenParticles = new List<ParticleSystemRenderer>();
    [FormerlySerializedAs("lightsAreOff")]
    [SerializeField, HideInInspector] private bool isOff = false;

    private MaterialPropertyBlock noGlow;

    // Read by the Inspector to show what's going on.
    public bool IsSunOnly { get { return isOff; } }
    public int LightsOffCount { get { return switchedOffLights.Count; } }
    public int GlowOffCount { get { return dimmedRenderers.Count; } }
    public int ParticlesOffCount { get { return hiddenParticles.Count; } }


    // Runs when the scene opens, and after scripts recompile.
    void OnEnable()
    {
        // The "no glow" setting isn't saved in the scene, so put it back.
        if (isOff) DimGlowingMaterials(dimmedRenderers);
    }


    void Update()
    {
        if (Application.isPlaying == false) return;

        Keyboard keys = Keyboard.current;
        if (keys != null && keys[toggleKey].wasPressedThisFrame)
        {
            if (isOff) AllLightsOn();
            else SunOnly();
        }
    }


    // =================================================================
    // SUN ONLY: switch everything else off
    // =================================================================

    public void SunOnly()
    {
        // If it's already in Sun Only, put everything back first so
        // nothing is counted twice.
        if (isOff) AllLightsOn();

        // ---- Lights ----
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (InThisScene(light) == false) continue;
            if (light.type == LightType.Directional) continue;   // keep the sun

            if (light.enabled)
            {
                light.enabled = false;
                switchedOffLights.Add(light);
                MarkChanged(light);
            }
        }

        // ---- Glowing materials ----
        if (alsoTurnOffGlow)
        {
            foreach (Renderer renderer in FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (InThisScene(renderer) == false) continue;
                if (renderer is ParticleSystemRenderer) continue;   // particles are handled below

                if (Glows(renderer))
                {
                    dimmedRenderers.Add(renderer);
                }
            }
            DimGlowingMaterials(dimmedRenderers);
        }

        // ---- Particles ----
        if (alsoTurnOffParticles)
        {
            foreach (ParticleSystemRenderer particles in FindObjectsByType<ParticleSystemRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (InThisScene(particles) == false) continue;

                if (particles.enabled)
                {
                    particles.enabled = false;
                    hiddenParticles.Add(particles);
                    MarkChanged(particles);
                }
            }
        }

        isOff = true;
        MarkChanged(this);
    }


    // =================================================================
    // ALL LIGHTS ON: put back everything SUN ONLY turned off
    // =================================================================

    public void AllLightsOn()
    {
        foreach (Light light in switchedOffLights)
        {
            if (light != null)
            {
                light.enabled = true;
                MarkChanged(light);
            }
        }

        foreach (Renderer renderer in dimmedRenderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(null);   // back to the material's own glow
            }
        }

        foreach (ParticleSystemRenderer particles in hiddenParticles)
        {
            if (particles != null)
            {
                particles.enabled = true;
                MarkChanged(particles);
            }
        }

        switchedOffLights.Clear();
        dimmedRenderers.Clear();
        hiddenParticles.Clear();
        isOff = false;
        MarkChanged(this);
    }


    // Lights in this scene that are off for some OTHER reason
    // (switched off by hand, or turned off by an older version of this script).
    public List<Light> OtherLightsThatAreOff()
    {
        List<Light> found = new List<Light>();
        foreach (Light light in FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (InThisScene(light) == false) continue;
            if (light.enabled == false && switchedOffLights.Contains(light) == false)
            {
                found.Add(light);
            }
        }
        return found;
    }


    public void TurnOn(List<Light> lightsToTurnOn)
    {
        foreach (Light light in lightsToTurnOn)
        {
            if (light != null)
            {
                light.enabled = true;
                MarkChanged(light);
            }
        }
    }


    // =================================================================
    // HELPERS
    // =================================================================

    // Does any material on this renderer have a glow (emission) colour?
    bool Glows(Renderer renderer)
    {
        foreach (Material material in renderer.sharedMaterials)
        {
            if (material == null) continue;
            if (material.HasProperty("_EmissionColor") == false) continue;

            Color glow = material.GetColor("_EmissionColor");
            if (glow.maxColorComponent > 0.01f) return true;
        }
        return false;
    }


    // Set the glow to black on these objects only. The material file is not changed.
    void DimGlowingMaterials(List<Renderer> renderers)
    {
        if (noGlow == null) noGlow = new MaterialPropertyBlock();
        noGlow.SetColor("_EmissionColor", Color.black);

        foreach (Renderer renderer in renderers)
        {
            if (renderer != null)
            {
                renderer.SetPropertyBlock(noGlow);
            }
        }
    }


    bool InThisScene(Component thing)
    {
        return thing.gameObject.scene == gameObject.scene;
    }


    // In the Editor, tell Unity the scene has changed so the change is saved.
    void MarkChanged(Object changed)
    {
#if UNITY_EDITOR
        if (Application.isPlaying == false)
        {
            UnityEditor.EditorUtility.SetDirty(changed);
            if (gameObject.scene.IsValid())
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
            }
        }
#endif
    }
}
