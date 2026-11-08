using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// =====================================================================
// SUN ONLY SWITCH: INSPECTOR  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT.
// It draws the SUN ONLY / ALL LIGHTS ON buttons in the Inspector for
// the Sun Only Switch component.
// =====================================================================
[CustomEditor(typeof(SunOnlySwitch))]
public class SunOnlySwitchEditor : Editor
{
    public override void OnInspectorGUI()
    {
        SunOnlySwitch lightSwitch = (SunOnlySwitch)target;

        // The two big buttons.
        EditorGUILayout.Space(6);
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(1f, 0.85f, 0.45f);
        if (GUILayout.Button("SUN ONLY", GUILayout.Height(40)))
        {
            lightSwitch.SunOnly();
        }

        GUI.backgroundColor = new Color(0.6f, 0.9f, 0.6f);
        if (GUILayout.Button("ALL LIGHTS ON", GUILayout.Height(40)))
        {
            lightSwitch.AllLightsOn();
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // What's happening right now.
        if (lightSwitch.IsSunOnly)
        {
            EditorGUILayout.HelpBox(
                "SUN ONLY: switched off " + lightSwitch.LightsOffCount + " lights, "
                + lightSwitch.GlowOffCount + " glowing objects and "
                + lightSwitch.ParticlesOffCount + " particle effects.",
                MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("ALL LIGHTS ON", MessageType.None);
        }

        // Lights that are off for some other reason.
        List<Light> otherOff = lightSwitch.OtherLightsThatAreOff();
        if (otherOff.Count > 0)
        {
            EditorGUILayout.Space(4);
            string names = "";
            foreach (Light light in otherOff)
            {
                names += "\n  " + light.name;
            }
            EditorGUILayout.HelpBox(otherOff.Count + " other light(s) are off but weren't turned off by this switch:" + names, MessageType.Warning);

            if (GUILayout.Button("Turn these on too"))
            {
                lightSwitch.TurnOn(otherOff);
            }
        }

        // The settings.
        EditorGUILayout.Space(8);
        serializedObject.Update();
        EditorGUILayout.PropertyField(serializedObject.FindProperty("alsoTurnOffGlow"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("alsoTurnOffParticles"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("toggleKey"));
        serializedObject.ApplyModifiedProperties();
    }
}
