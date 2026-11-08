using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// =====================================================================
// LIGHTS DEMO BUILDER  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT. You only need to USE it.
//
// How to use it:
//   Tools > CRE134 > Build Lights Demo
//
// What it makes:
//   "Lights Demo - Living Room" in  Assets/1. Scenes/1. For Teaching
//   A small living room (Kenney furniture) with ONE of each light type,
//   to go with the "Adding Lights in Unity" slides:
//
//     LIGHTS
//       1 Directional Light (Sun)        the sun: only rotation matters
//       2 Point Light (Floor Lamp)       inside FURNITURE > lampRoundFloor,
//                                        so it moves with the lamp
//       3 Spot Light (Stage)             a cone over the test shapes
//       4 Area Light (Window)            Baked only: click Generate Lighting
//     FROM CHRISTMAS COTTAGE             the fireplace and tree, copied with
//                                        their point lights, flicker and flames
//     CHECKLIST PROBLEMS (fix me)        switched OFF; three broken lights
//                                        for the Lighting Checklist slide
//
//   Every group can be ticked on and off in the Hierarchy.
//   Ambient light is set in Window > Rendering > Lighting > Environment.
//
//   Running it again rebuilds the scene from scratch.
// =====================================================================
public static class LightsDemoBuilder
{
    const string Folder = "Assets/LightsDemo";
    const string MatFolder = Folder + "/Materials";
    const string SceneFolder = "Assets/1. Scenes/1. For Teaching";
    const string SceneName = "Lights Demo - Living Room";

    // The room, inside the walls: x from -5 to 5, z from -4 to 4, 3 m high.
    const float HalfX = 5f;
    const float HalfZ = 4f;
    const float WallHeight = 3f;
    const float Wall = 0.3f;

    // The tree is moved this far from where it stood in the cottage.
    static readonly Vector3 TreeOffset = new Vector3(0.6f, 0f, -0.3f);

    static readonly Vector3 CameraPosition = new Vector3(6.2f, 4.0f, -7.2f);
    static readonly Vector3 CameraTarget = new Vector3(-0.6f, 0.9f, 0.8f);


    [MenuItem("Tools/CRE134/Build Lights Demo")]
    public static void Build()
    {
        string scenePath = SceneFolder + "/" + SceneName + ".unity";

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("Lights Demo",
                "The Lights Demo scene already exists. Rebuild it from scratch?\n\nAny changes you made to it will be lost.", "Rebuild", "Cancel");
            if (rebuild == false) return;
        }

        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false) return;

        MakeFolder("Assets", "LightsDemo");
        MakeFolder(Folder, "Materials");
        MakeFolder("Assets", "1. Scenes");
        MakeFolder("Assets/1. Scenes", "1. For Teaching");

        // ---- Materials: plain colours, so the light's colour is easy to see ----
        Material floorMat = Mat("Floor - Warm Wood", new Color(0.55f, 0.40f, 0.28f), 0.35f);
        Material wallMat = Mat("Wall - Off White", new Color(0.86f, 0.84f, 0.80f), 0.1f);
        Material frameMat = Mat("Window Frame", new Color(0.30f, 0.24f, 0.20f), 0.3f);
        Material whiteMat = Mat("Test - White", new Color(0.92f, 0.92f, 0.92f), 0.5f);
        Material greyMat = Mat("Test - Grey", new Color(0.55f, 0.56f, 0.58f), 0.2f);
        Material plinthMat = Mat("Test - Plinth", new Color(0.25f, 0.26f, 0.30f), 0.2f);

        // ---- Scene ----
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(scene, scenePath);

        Transform room = new GameObject("ROOM").transform;
        Transform furniture = new GameObject("FURNITURE").transform;
        Transform testShapes = new GameObject("TEST SHAPES").transform;
        Transform lights = new GameObject("LIGHTS").transform;
        Transform problems = new GameObject("CHECKLIST PROBLEMS (fix me)").transform;
        Transform labels = new GameObject("LABELS").transform;

        BuildRoom(room, floorMat, wallMat, frameMat);
        BuildFurniture(furniture, out Transform floorLamp);
        BuildTestShapes(testShapes, whiteMat, greyMat, plinthMat);

        // ---- The fireplace and tree, copied from the Christmas Cottage ----
        Transform christmas = new GameObject("FROM CHRISTMAS COTTAGE").transform;
        CopyFromCottage(scene, christmas);

        // ---- 1 Directional Light: the sun ----
        GameObject sunObject = new GameObject("1 Directional Light (Sun)");
        sunObject.transform.SetParent(lights, false);
        sunObject.transform.position = new Vector3(0f, 5f, -2f);   // position does nothing, it's just easy to find here
        sunObject.transform.rotation = Quaternion.Euler(42f, 35f, 0f);
        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = new Color(1f, 0.96f, 0.90f);
        sun.intensity = 1f;
        sun.shadows = LightShadows.Soft;
        sun.lightmapBakeType = LightmapBakeType.Realtime;
        RenderSettings.sun = sun;

        // ---- 2 Point Light: a child of the floor lamp, at the shade ----
        GameObject pointObject = new GameObject("2 Point Light (Floor Lamp)");
        pointObject.transform.SetParent(floorLamp, true);
        pointObject.transform.position = floorLamp.position + new Vector3(0f, 1.45f, 0f);
        Light point = pointObject.AddComponent<Light>();
        point.type = LightType.Point;
        point.color = new Color(1f, 0.72f, 0.42f);
        point.intensity = 3f;
        point.range = 6f;
        point.shadows = LightShadows.Soft;
        point.lightmapBakeType = LightmapBakeType.Realtime;

        // The real light lives INSIDE the lamp (FURNITURE > lampRoundFloor)
        // so it moves with the lamp, like adding a Light with Add Component.

        // ---- 3 Spot Light: a stage light over the test shapes ----
        GameObject spotObject = new GameObject("3 Spot Light (Stage)");
        spotObject.transform.SetParent(lights, false);
        spotObject.transform.position = new Vector3(2.0f, 2.9f, -2.4f);
        spotObject.transform.LookAt(new Vector3(1.6f, 0f, -0.8f));
        Light spot = spotObject.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.color = new Color(0.92f, 0.95f, 1f);
        spot.intensity = 6f;
        spot.range = 7f;
        spot.spotAngle = 45f;
        spot.innerSpotAngle = 30f;
        spot.shadows = LightShadows.Soft;
        spot.lightmapBakeType = LightmapBakeType.Realtime;

        // ---- 4 Area Light: the window. Baked only in URP. ----
        GameObject areaObject = new GameObject("4 Area Light (Window) - Baked only");
        areaObject.transform.SetParent(lights, false);
        areaObject.transform.position = new Vector3(-HalfX + 0.05f, 1.7f, 0f);
        areaObject.transform.rotation = Quaternion.Euler(0f, 90f, 0f);   // shine into the room (+X)
        Light area = areaObject.AddComponent<Light>();
        area.type = LightType.Rectangle;
        area.areaSize = new Vector2(2f, 1.4f);
        area.color = new Color(0.78f, 0.87f, 1f);
        area.intensity = 4f;
        area.range = 10f;
        area.lightmapBakeType = LightmapBakeType.Baked;

        // ---- Checklist problems: three broken lights, switched OFF ----
        // Tiny range: sits right beside the shapes but reaches almost nothing.
        Light tiny = AddPoint("Fix Me 1 - Tiny Range (try Range 4)", problems, new Vector3(0.6f, 1.2f, -1.6f), new Color(1f, 0.45f, 0.25f), 0.3f, 4f);
        // Zero intensity: everything else is right.
        Light zero = AddPoint("Fix Me 2 - Zero Intensity (try 3)", problems, new Vector3(2.6f, 1.2f, -0.2f), new Color(0.35f, 0.6f, 1f), 4f, 0f);
        // Too far away: a good light, but out in the garden.
        Light far = AddPoint("Fix Me 3 - Too Far Away (move it to the shapes)", problems, new Vector3(14f, 1.5f, -9f), new Color(0.5f, 1f, 0.5f), 4f, 3f);
        far.shadows = LightShadows.None;
        tiny.shadows = LightShadows.None;
        zero.shadows = LightShadows.None;
        problems.gameObject.SetActive(false);

        // ---- Ambient light and sky ----
        // Ambient is NOT a light object. It lives in the Lighting window.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.24f, 0.26f, 0.31f);
        RenderSettings.skybox = FindMaterial("FS002_Day", "LG_Sky_Midday");
        RenderSettings.fog = false;

        // ---- Light probes, so the furniture picks up the baked window light ----
        AddLightProbes(lights);

        // ---- Lighting settings: quick, low-resolution bake for class ----
        LightingSettings settings = new LightingSettings();
        settings.name = SceneName + " Lighting";
        settings.bakedGI = true;
        settings.realtimeGI = false;
        settings.autoGenerate = false;
        settings.lightmapper = LightingSettings.Lightmapper.ProgressiveGPU;
        settings.lightmapResolution = 12f;
        settings.lightmapMaxSize = 1024;
        settings.directSampleCount = 16;
        settings.indirectSampleCount = 128;
        settings.environmentSampleCount = 64;
        settings.maxBounces = 2;
        settings.mixedBakeMode = MixedLightingMode.IndirectOnly;
        string settingsPath = Folder + "/" + SceneName + " Lighting.lighting";
        AssetDatabase.DeleteAsset(settingsPath);
        AssetDatabase.CreateAsset(settings, settingsPath);
        Lightmapping.lightingSettings = settings;

        // ---- Labels in the world ----
        Label(labels, "1  Directional Light\n<size=70%>The sun. Only its rotation matters.</size>", sunObject.transform.position + Vector3.up * 0.6f);
        Label(labels, "2  Point Light\n<size=70%>Lamp: shines every way. Set its Range.</size>", pointObject.transform.position + Vector3.up * 0.7f);
        Label(labels, "3  Spot Light\n<size=70%>A cone. Position, direction and angle matter.</size>", spotObject.transform.position + Vector3.up * 0.6f);
        Label(labels, "Point Lights in the fireplace\n<size=70%>Fire (flickers) and candles. Warm, short Range.</size>", new Vector3(0f, 3.5f, 3.6f));
        Label(labels, "Point Lights in the tree\n<size=70%>Small glows and a star. Emissive bulbs aren't lights!</size>", TreeOffset + new Vector3(3.5f, 4.0f, 3f));
        Label(labels, "4  Area Light\n<size=70%>Window. Baked only: Generate Lighting.</size>", new Vector3(-HalfX + 0.4f, WallHeight + 0.6f, 0f));

        // ---- Sun Only Switch: buttons in the Inspector to leave just the sun on ----
        GameObject switchObject = new GameObject("SUN ONLY SWITCH");
        switchObject.AddComponent<SunOnlySwitch>();
        switchObject.transform.SetAsFirstSibling();

        // ---- Camera ----
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.fieldOfView = 50f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = CameraPosition;
        cameraObject.transform.LookAt(CameraTarget);

        // Point the Scene view the same way.
        SceneView view = SceneView.lastActiveSceneView;
        if (view != null)
        {
            view.LookAtDirect(CameraTarget, cameraObject.transform.rotation, 9f);
            view.sceneLighting = true;
        }

        EditorSceneManager.SaveScene(scene, scenePath);
        Selection.activeGameObject = lights.gameObject;
        EditorGUIUtility.PingObject(lights.gameObject);

        EditorUtility.DisplayDialog("Lights Demo",
            "Built " + scenePath + "\n\n" +
            "Tick the groups under LIGHTS on and off to show each light type.\n\n" +
            "The Area Light (window) shows nothing until you click\n" +
            "Window > Rendering > Lighting > Generate Lighting.\n\n" +
            "CHECKLIST PROBLEMS is switched off: tick it on to fix the broken lights live.", "OK");
    }


    // =================================================================
    // ROOM: floor, back wall, and a left wall with a window hole
    // =================================================================

    static void BuildRoom(Transform room, Material floorMat, Material wallMat, Material frameMat)
    {
        float wallX = -HalfX - Wall / 2f;
        float wallZ = HalfZ + Wall / 2f;
        float halfH = WallHeight / 2f;

        StaticBox("Floor", room, new Vector3(0f, -0.1f, 0f), new Vector3(HalfX * 2f + Wall * 2f, 0.2f, HalfZ * 2f + Wall * 2f), floorMat);
        StaticBox("Wall Back", room, new Vector3(-Wall / 2f, halfH, wallZ), new Vector3(HalfX * 2f + Wall, WallHeight, Wall), wallMat);

        // The window hole: z from -1 to 1, y from 1.0 to 2.4.
        float holeZ = 1f;
        float sill = 1f;
        float top = 2.4f;
        float sideLength = HalfZ - holeZ;

        StaticBox("Wall Left - Below Window", room, new Vector3(wallX, sill / 2f, 0f), new Vector3(Wall, sill, HalfZ * 2f), wallMat);
        StaticBox("Wall Left - Above Window", room, new Vector3(wallX, (top + WallHeight) / 2f, 0f), new Vector3(Wall, WallHeight - top, HalfZ * 2f), wallMat);
        StaticBox("Wall Left - Front", room, new Vector3(wallX, (sill + top) / 2f, -holeZ - sideLength / 2f), new Vector3(Wall, top - sill, sideLength), wallMat);
        StaticBox("Wall Left - Back", room, new Vector3(wallX, (sill + top) / 2f, holeZ + sideLength / 2f), new Vector3(Wall, top - sill, sideLength), wallMat);

        // A thin frame and sill so it reads as a window.
        Transform frame = new GameObject("Window Frame").transform;
        frame.SetParent(room, false);
        StaticBox("Sill", frame, new Vector3(wallX + 0.1f, sill - 0.03f, 0f), new Vector3(Wall + 0.2f, 0.06f, holeZ * 2f + 0.2f), frameMat);
        StaticBox("Top", frame, new Vector3(wallX, top + 0.03f, 0f), new Vector3(Wall + 0.04f, 0.06f, holeZ * 2f + 0.12f), frameMat);
        StaticBox("Side Front", frame, new Vector3(wallX, (sill + top) / 2f, -holeZ - 0.03f), new Vector3(Wall + 0.04f, top - sill, 0.06f), frameMat);
        StaticBox("Side Back", frame, new Vector3(wallX, (sill + top) / 2f, holeZ + 0.03f), new Vector3(Wall + 0.04f, top - sill, 0.06f), frameMat);
        StaticBox("Middle Bar", frame, new Vector3(wallX, (sill + top) / 2f, 0f), new Vector3(0.05f, top - sill, 0.05f), frameMat);
    }


    // =================================================================
    // FURNITURE (Kenney Furniture Kit)
    // =================================================================

    static void BuildFurniture(Transform furniture, out Transform floorLamp)
    {
        floorLamp = null;

        // name, position, turn (degrees), height in metres
        GameObject lamp = Furniture(furniture, "lampRoundFloor", new Vector3(-4.4f, 0f, 3.65f), 0f, 1.65f);
        if (lamp != null)
        {
            floorLamp = lamp.transform;

            // The light sits inside the shade, so the shade mustn't cast
            // a shadow or it would block its own light.
            foreach (MeshRenderer renderer in lamp.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }
        }
        else
        {
            // No Kenney lamp found: use a simple pole instead.
            floorLamp = new GameObject("Floor Lamp (stand-in)").transform;
            floorLamp.SetParent(furniture, false);
            floorLamp.position = new Vector3(-4.4f, 0f, 3.65f);
        }

        // Laid out around the fireplace (back wall, middle) and the tree (back right).
        Furniture(furniture, "loungeSofa", new Vector3(-4.4f, 0f, 2.0f), 90f, 0.85f);
        Primitive(PrimitiveType.Cube, "Rug", furniture, new Vector3(-2.3f, 0.01f, 2.0f), new Vector3(3.4f, 0.02f, 2.2f), Mat("Rug", new Color(0.45f, 0.20f, 0.18f), 0.05f));
        Furniture(furniture, "tableCoffee", new Vector3(-2.9f, 0f, 2.0f), 90f, 0.42f);
        Furniture(furniture, "loungeChair", new Vector3(-1.6f, 0f, 1.6f), 40f, 0.85f);
        Furniture(furniture, "bookcaseOpen", new Vector3(2.45f, 0f, 3.65f), 180f, 2.0f);
        Furniture(furniture, "tableRound", new Vector3(-3.6f, 0f, -0.6f), 0f, 0.75f);
        Furniture(furniture, "chair", new Vector3(-3.6f, 0f, -1.6f), 0f, 0.9f);
        Furniture(furniture, "chair", new Vector3(-2.6f, 0f, -0.6f), 270f, 0.9f);
        Furniture(furniture, "pottedPlant", new Vector3(-4.4f, 0f, -3.4f), 0f, 1.1f);
        Furniture(furniture, "plantSmall2", new Vector3(-3.6f, 0.75f, -0.6f), 0f, 0.3f);
    }


    // Place one Kenney model and make it part of the baked lighting.
    static GameObject Furniture(Transform parent, string modelName, Vector3 position, float yaw, float height)
    {
        string path = AreaKit.FindModel(modelName);
        if (path == null)
        {
            Debug.LogWarning("Lights Demo: couldn't find the Kenney model '" + modelName + "'.");
            return null;
        }

        GameObject placed = AreaKit.Model(path, parent, position, yaw, height);
        if (placed == null) return null;

        // Kenney models have no lightmap UVs, so they take baked light
        // from the light probes instead of from a lightmap.
        foreach (MeshRenderer renderer in placed.GetComponentsInChildren<MeshRenderer>())
        {
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
            renderer.receiveGI = ReceiveGI.LightProbes;
        }

        return placed;
    }


    // =================================================================
    // COPY FROM THE CHRISTMAS COTTAGE
    // Opens the cottage scene alongside, duplicates the Fireplace and
    // Christmas Tree (with their lights, flicker script and flames),
    // moves the copies into this scene, then closes the cottage WITHOUT
    // saving, so the cottage itself is not changed.
    // The cottage room is the same size as this room, so the fireplace
    // lands on the back wall in exactly the same place.
    // =================================================================

    static void CopyFromCottage(Scene demoScene, Transform parent)
    {
        string cottagePath = null;
        foreach (string guid in AssetDatabase.FindAssets("Christmas Cottage t:Scene"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == "Christmas Cottage") cottagePath = path;
        }

        if (cottagePath == null)
        {
            Debug.LogWarning("Lights Demo: couldn't find the Christmas Cottage scene, so the fireplace and tree were not copied.");
            return;
        }

        Scene cottage = EditorSceneManager.OpenScene(cottagePath, OpenSceneMode.Additive);

        string[] wanted = { "Fireplace", "Christmas Tree" };
        foreach (string objectName in wanted)
        {
            GameObject original = FindInScene(cottage, objectName);
            if (original == null)
            {
                Debug.LogWarning("Lights Demo: no '" + objectName + "' in the Christmas Cottage.");
                continue;
            }

            // Duplicate the same way Ctrl+D does, so prefabs inside stay prefabs.
            Selection.objects = new Object[] { original };
            Unsupported.DuplicateGameObjectsUsingPasteboard();
            GameObject copy = Selection.activeGameObject;
            if (copy == null || copy == original) continue;

            copy.name = objectName;
            copy.transform.SetParent(null, true);
            SceneManager.MoveGameObjectToScene(copy, demoScene);
            copy.transform.SetParent(parent, true);

            if (objectName == "Christmas Tree")
            {
                copy.transform.position = copy.transform.position + TreeOffset;
            }
        }

        EditorSceneManager.CloseScene(cottage, true);
        SceneManager.SetActiveScene(demoScene);
    }


    static GameObject FindInScene(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (child.name == objectName) return child.gameObject;
            }
        }
        return null;
    }


    // =================================================================
    // TEST SHAPES: easy to read under the spot light
    // =================================================================

    static void BuildTestShapes(Transform parent, Material white, Material grey, Material plinth)
    {
        Vector3 centre = new Vector3(1.6f, 0f, -0.8f);

        Primitive(PrimitiveType.Cylinder, "Plinth", parent, centre + new Vector3(0f, 0.1f, 0f), new Vector3(2.2f, 0.1f, 2.2f), plinth);
        Primitive(PrimitiveType.Sphere, "Sphere", parent, centre + new Vector3(-0.45f, 0.6f, 0.1f), Vector3.one * 0.8f, white);
        Primitive(PrimitiveType.Cube, "Cube", parent, centre + new Vector3(0.45f, 0.5f, 0.25f), Vector3.one * 0.6f, grey);
        Primitive(PrimitiveType.Capsule, "Capsule", parent, centre + new Vector3(0.1f, 0.7f, -0.5f), new Vector3(0.4f, 0.5f, 0.4f), white);
    }


    // =================================================================
    // SMALL HELPERS
    // =================================================================

    static Material Mat(string materialName, Color colour, float smoothness)
    {
        string path = MatFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, path);
        }

        material.SetColor("_BaseColor", colour);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(material);
        return material;
    }


    // A Unity cube, sized and placed, marked Static so baked light lands on it.
    static GameObject StaticBox(string objectName, Transform parent, Vector3 centre, Vector3 size, Material material)
    {
        GameObject box = Primitive(PrimitiveType.Cube, objectName, parent, centre, size, material);
        GameObjectUtility.SetStaticEditorFlags(box, StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic);
        return box;
    }


    static GameObject Primitive(PrimitiveType type, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject shape = GameObject.CreatePrimitive(type);
        shape.name = objectName;
        shape.transform.SetParent(parent, false);
        shape.transform.position = position;
        shape.transform.localScale = scale;
        shape.GetComponent<MeshRenderer>().sharedMaterial = material;
        return shape;
    }


    static Light AddPoint(string lightName, Transform parent, Vector3 position, Color colour, float range, float intensity)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.position = position;
        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = colour;
        pointLight.range = range;
        pointLight.intensity = intensity;
        pointLight.lightmapBakeType = LightmapBakeType.Realtime;
        return pointLight;
    }


    // A floating label that faces the camera.
    static void Label(Transform parent, string text, Vector3 position)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;

        Vector3 toLabel = position - CameraPosition;
        toLabel.y = 0f;
        labelObject.transform.rotation = Quaternion.LookRotation(toLabel);

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = 3.2f;
        label.fontStyle = FontStyles.Bold;
        label.color = new Color(0.10f, 0.12f, 0.20f);
        label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(4.5f, 1.2f);
        label.textWrappingMode = TextWrappingModes.Normal;
    }


    // A grid of light probes across the room.
    static void AddLightProbes(Transform parent)
    {
        GameObject probesObject = new GameObject("Light Probes (carry the baked window light to the furniture)");
        probesObject.transform.SetParent(parent, false);
        LightProbeGroup group = probesObject.AddComponent<LightProbeGroup>();

        List<Vector3> positions = new List<Vector3>();
        for (float x = -HalfX + 0.5f; x <= HalfX - 0.4f; x += 1.5f)
        {
            for (float z = -HalfZ + 0.5f; z <= HalfZ - 0.4f; z += 1.5f)
            {
                positions.Add(new Vector3(x, 0.3f, z));
                positions.Add(new Vector3(x, 1.4f, z));
                positions.Add(new Vector3(x, 2.6f, z));
            }
        }
        group.probePositions = positions.ToArray();
    }


    // Find a material by name anywhere in Assets; try each name in turn.
    static Material FindMaterial(params string[] names)
    {
        foreach (string materialName in names)
        {
            foreach (string guid in AssetDatabase.FindAssets(materialName + " t:Material"))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == materialName)
                {
                    return AssetDatabase.LoadAssetAtPath<Material>(path);
                }
            }
        }
        return null;
    }


    static void MakeFolder(string parent, string folderName)
    {
        if (AssetDatabase.IsValidFolder(parent + "/" + folderName) == false)
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
