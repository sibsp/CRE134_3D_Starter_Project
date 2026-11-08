using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// =====================================================================
// AREA KIT  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT.
// It lives in an "Editor" folder, so it only runs inside the Unity
// Editor and is never part of your game. It was used to BUILD the
// Learning Areas scenes, so that every area has the same player, the
// same on-screen UI and the same style of notice board.
//
// What it does:
//   It is a box of helper methods: make a scene, make a material, make a
//   room, place a model, add lights, add text, add trigger zones...
//
// "static" means you don't add this to a GameObject. You call the
// methods by the class name, for example:  AreaKit.Save();
// =====================================================================
public static class AreaKit
{
    // ---------- Settings ----------

    // Where new scenes are saved.
    public const string SceneFolder = "Assets/Scenes/Learning Areas";

    // Where new materials and textures are saved.
    public const string MatFolder = "Assets/LearningAreas/Materials";

    // The top object of the scene being built. New objects go inside it.
    public static Transform Root;

    // ---------- Private variables ----------

    // The UModeler X type that rebuilds a shape's mesh (found the first time we need it).
    private static System.Type rebuilder;


    // =================================================================
    // SCENES
    // =================================================================

    // Make a new, empty scene and save it in the Learning Areas folder.
    public static Scene NewScene(string sceneName)
    {
        // Save the scene that is open now, so nothing is lost.
        Scene current = SceneManager.GetActiveScene();
        if (current.isDirty)
        {
            EditorSceneManager.SaveScene(current);
        }

        // Make the folder if it doesn't exist yet.
        if (AssetDatabase.IsValidFolder(SceneFolder) == false)
        {
            AssetDatabase.CreateFolder("Assets/Scenes", "Learning Areas");
        }

        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorSceneManager.SaveScene(newScene, SceneFolder + "/" + sceneName + ".unity");

        // An empty object at the top that everything else goes inside.
        GameObject rootObject = new GameObject("--- " + sceneName.ToUpper() + " ---");
        Root = rootObject.transform;

        return newScene;
    }


    // Save the open scene.
    public static void Save()
    {
        Scene current = SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(current);
        EditorSceneManager.SaveScene(current);
    }


    // Make an empty object to group other objects together (like a folder).
    // If parent is null, it goes inside Root.
    public static Transform Group(string groupName, Transform parent)
    {
        GameObject groupObject = new GameObject(groupName);
        groupObject.transform.SetParent(ParentOrRoot(parent), false);
        return groupObject.transform;
    }


    // Make a group inside Root.
    public static Transform Group(string groupName)
    {
        return Group(groupName, null);
    }


    // If parent is null, use Root instead.
    private static Transform ParentOrRoot(Transform parent)
    {
        if (parent != null)
        {
            return parent;
        }

        return Root;
    }


    // =================================================================
    // MATERIALS AND TEXTURES
    // =================================================================

    // Make (or update) a simple URP Lit material.
    public static Material Mat(string materialName, Color colour, float smoothness)
    {
        return Mat(materialName, colour, smoothness, 0f);
    }


    // Make (or update) a URP Lit material with smoothness and metallic.
    public static Material Mat(string materialName, Color colour, float smoothness, float metallic)
    {
        string path = MatFolder + "/" + materialName + ".mat";

        // Is there already a material with this name?
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        bool isNew = false;

        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            isNew = true;
        }

        material.SetColor("_BaseColor", colour);
        material.SetFloat("_Smoothness", smoothness);
        material.SetFloat("_Metallic", metallic);

        if (isNew == true)
        {
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            EditorUtility.SetDirty(material);
        }

        return material;
    }


    // Make a material that glows (emission).
    public static Material GlowMat(string materialName, Color colour, float smoothness, Color glowColour)
    {
        Material material = Mat(materialName, colour, smoothness, 0f);

        material.EnableKeyword("_EMISSION");
        material.SetColor("_EmissionColor", glowColour);
        material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
        EditorUtility.SetDirty(material);

        return material;
    }


    // Make a see-through (transparent) material. Use a colour with alpha below 1.
    public static Material SeeThroughMat(string materialName, Color colour, float smoothness)
    {
        Material material = Mat(materialName, colour, smoothness, 0f);

        material.SetFloat("_Surface", 1f);   // 1 = Transparent
        material.SetFloat("_Blend", 0f);     // 0 = Alpha blending
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.renderQueue = 3000;         // transparent things are drawn last
        EditorUtility.SetDirty(material);

        return material;
    }


    // Make a material that uses a texture, repeated "tiling" times.
    public static Material TexturedMat(string materialName, Color colour, float smoothness, Texture texture, Vector2 tiling)
    {
        Material material = Mat(materialName, colour, smoothness, 0f);

        material.SetTexture("_BaseMap", texture);
        material.SetTextureScale("_BaseMap", tiling);
        EditorUtility.SetDirty(material);

        return material;
    }


    // Make a simple pattern texture and save it as a PNG.
    // pattern can be "checker", "tiles", "planks" or "bricks".
    // colourA is the main colour, colourB is the lines between.
    public static Texture2D PatternTexture(string textureName, Color colourA, Color colourB, string pattern)
    {
        string path = MatFolder + "/" + textureName + ".png";

        // If we already made it, use that one.
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (existing != null)
        {
            return existing;
        }

        int size = 256;
        Texture2D texture = new Texture2D(size, size);

        // Colour every pixel, one row (y) at a time.
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color pixel = PatternPixel(x, y, size, colourA, colourB, pattern);
                texture.SetPixel(x, y, pixel);
            }
        }

        // Save the picture as a PNG file, then import it into Unity.
        System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
        AssetDatabase.ImportAsset(path);

        // Make it repeat when tiled.
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.anisoLevel = 4;
        importer.SaveAndReimport();

        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }


    // Work out the colour of one pixel of a pattern.
    private static Color PatternPixel(int x, int y, int size, Color colourA, Color colourB, string pattern)
    {
        if (pattern == "checker")
        {
            // Two big squares across and two down.
            int squareX = x / (size / 2);
            int squareY = y / (size / 2);

            if ((squareX + squareY) % 2 == 0)
            {
                return colourA;
            }
            return colourB;
        }

        if (pattern == "tiles")
        {
            // 4 x 4 tiles with a 4-pixel line between them.
            int tileSize = size / 4;

            if (x % tileSize < 4 || y % tileSize < 4)
            {
                return colourB;
            }
            return colourA;
        }

        if (pattern == "planks")
        {
            int plankHeight = size / 4;
            int plankRow = y / plankHeight;

            // Each row's join is in a different place.
            int joinX = (x + plankRow * 37) % size;

            if (y % plankHeight < 3 || joinX < 3)
            {
                return colourB;
            }

            // A little Perlin noise makes it look like wood grain.
            float grain = Mathf.PerlinNoise(x * 0.02f, y * 0.3f) * 0.25f;
            return Color.Lerp(colourA, colourB, grain);
        }

        if (pattern == "bricks")
        {
            int brickHeight = size / 8;
            int brickWidth = size / 4;
            int brickRow = y / brickHeight;

            // Every other row is shifted along by half a brick.
            int shift = 0;
            if (brickRow % 2 == 1)
            {
                shift = size / 8;
            }

            if (y % brickHeight < 4 || (x + shift) % brickWidth < 4)
            {
                return colourB;
            }

            float roughness = Mathf.PerlinNoise(x * 0.05f, y * 0.05f) * 0.3f;
            return Color.Lerp(colourA, colourB, roughness);
        }

        // Unknown pattern: plain colour.
        return colourA;
    }


    // =================================================================
    // SHAPES
    // =================================================================

    // Make a UModeler X "Parametric Shape" (e.g. "Box", "Cylinder", "Stairs").
    //   bottomCentre  where the BOTTOM-middle of the shape should be
    //   angles        the rotation
    //   settings      the shape's settings by name, e.g. "Width" = 2
    //
    // UModeler X is a separate package, so this script can't name its
    // classes directly. Instead it uses "reflection": finding methods and
    // properties by their NAME while the Editor is running.
    public static GameObject Shape(string shapeType, string objectName, Transform parent, Vector3 bottomCentre, Vector3 angles, Material material, Dictionary<string, object> settings, bool addCollider)
    {
        // 1. Make the shape using UModeler X's own menu item.
        Selection.activeObject = null;
        EditorApplication.ExecuteMenuItem("GameObject/3D Object/Parametric Shape (Beta)");
        GameObject shapeObject = Selection.activeGameObject;
        shapeObject.name = objectName;
        shapeObject.transform.SetParent(ParentOrRoot(parent), false);

        // 2. Find the UMXParametricShape component on it.
        Component shape = FindComponentByName(shapeObject, "UMXParametricShape");
        System.Type shapeClass = shape.GetType();

        // 3. Choose which shape it is (Box, Cylinder...).
        PropertyInfo shapeTypeProperty = shapeClass.GetProperty("ShapeType");
        object shapeTypeValue = System.Enum.Parse(shapeTypeProperty.PropertyType, shapeType);
        MethodInfo resetMethod = shapeClass.GetMethod("ResetToDefaults");
        resetMethod.Invoke(shape, new object[] { shapeTypeValue });

        // 4. Copy in each setting (Width, Height, ...).
        if (settings != null)
        {
            foreach (KeyValuePair<string, object> setting in settings)
            {
                PropertyInfo property = shapeClass.GetProperty(setting.Key);
                object value = setting.Value;

                // Settings that are a list of choices (enums) can be given as text.
                if (property.PropertyType.IsEnum && value is string)
                {
                    value = System.Enum.Parse(property.PropertyType, (string)value);
                }

                property.SetValue(shape, value);
            }
        }

        if (material != null)
        {
            shapeClass.GetProperty("Material").SetValue(shape, material);
        }

        // 5. Ask UModeler X to build the mesh now.
        MethodInfo rebuildMethod = FindRebuildMethod();
        rebuildMethod.Invoke(null, new object[] { shape });

        // 6. Rotate it, then move it so its bottom-middle is at bottomCentre.
        shapeObject.transform.rotation = Quaternion.Euler(angles);

        Mesh mesh = shapeObject.GetComponent<MeshFilter>().sharedMesh;
        Bounds meshBounds = mesh.bounds;
        Vector3 bottomOfMesh = new Vector3(meshBounds.center.x, meshBounds.min.y, meshBounds.center.z);
        shapeObject.transform.position = bottomCentre - shapeObject.transform.rotation * bottomOfMesh;

        if (material != null)
        {
            shapeObject.GetComponent<MeshRenderer>().sharedMaterial = material;
        }

        if (addCollider == true)
        {
            MeshCollider meshCollider = shapeObject.AddComponent<MeshCollider>();
            meshCollider.sharedMesh = mesh;
        }

        return shapeObject;
    }


    // Find a component on an object by its class name.
    private static Component FindComponentByName(GameObject target, string className)
    {
        Component[] components = target.GetComponents<Component>();

        for (int i = 0; i < components.Length; i++)
        {
            if (components[i].GetType().Name == className)
            {
                return components[i];
            }
        }

        return null;
    }


    // Find UModeler X's "RebuildNow" method (we only search the first time).
    private static MethodInfo FindRebuildMethod()
    {
        if (rebuilder == null)
        {
            // Look through every loaded assembly (code library) for UModeler's.
            Assembly[] assemblies = System.AppDomain.CurrentDomain.GetAssemblies();

            for (int i = 0; i < assemblies.Length; i++)
            {
                if (assemblies[i].GetName().Name.StartsWith("Tripolygon") == false)
                {
                    continue;
                }

                System.Type found = assemblies[i].GetType("UMXParametricShapeRebuilder");
                if (found == null)
                {
                    // It may be inside a namespace, so check every type's short name.
                    System.Type[] types = assemblies[i].GetTypes();
                    for (int t = 0; t < types.Length; t++)
                    {
                        if (types[t].Name == "UMXParametricShapeRebuilder")
                        {
                            found = types[t];
                        }
                    }
                }

                if (found != null)
                {
                    rebuilder = found;
                    break;
                }
            }
        }

        // RebuildNow may be public or private, so ask for both.
        BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
        MethodInfo[] methods = rebuilder.GetMethods(flags);

        for (int i = 0; i < methods.Length; i++)
        {
            if (methods[i].Name == "RebuildNow" && methods[i].GetParameters().Length == 1)
            {
                return methods[i];
            }
        }

        return null;
    }


    // Make a UModeler X box. centre is the MIDDLE of the box.
    public static GameObject Box(string objectName, Transform parent, Vector3 centre, Vector3 size, Material material)
    {
        return Box(objectName, parent, centre, size, material, 0f, true);
    }


    // Make a UModeler X box, turned by "yaw" degrees.
    public static GameObject Box(string objectName, Transform parent, Vector3 centre, Vector3 size, Material material, float yaw, bool addCollider)
    {
        Dictionary<string, object> settings = new Dictionary<string, object>();
        settings.Add("Width", size.x);
        settings.Add("Height", size.y);
        settings.Add("Depth", size.z);

        // Shape() wants the bottom of the box, so go down half its height.
        Vector3 bottomCentre = centre - new Vector3(0f, size.y / 2f, 0f);
        Vector3 angles = new Vector3(0f, yaw, 0f);

        return Shape("Box", objectName, parent, bottomCentre, angles, material, settings, addCollider);
    }


    // Make one of Unity's built-in shapes (Cube, Sphere, Cylinder...).
    public static GameObject Prim(PrimitiveType primitiveType, string objectName, Transform parent, Vector3 position, Vector3 scale, Material material, bool addCollider, float yaw)
    {
        GameObject primitive = GameObject.CreatePrimitive(primitiveType);
        primitive.name = objectName;
        primitive.transform.SetParent(ParentOrRoot(parent), false);
        primitive.transform.position = position;
        primitive.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        primitive.transform.localScale = scale;

        if (material != null)
        {
            primitive.GetComponent<Renderer>().sharedMaterial = material;
        }

        // Built-in shapes come with a collider. Remove it if we don't want one.
        if (addCollider == false)
        {
            Object.DestroyImmediate(primitive.GetComponent<Collider>());
        }

        return primitive;
    }


    // Make a room: a floor, four walls and a ceiling, with a doorway in the front (-Z) wall.
    //   centre       the middle of the room at floor level
    //   doorWidth    0 = no doorway
    public static Transform Room(string roomName, Vector3 centre, Vector3 size, Material wall, Material floor, Material ceiling, float doorWidth, float doorHeight, bool addCeiling)
    {
        Transform room = Group(roomName);
        float thickness = 0.3f;

        // Floor and ceiling are a little bigger than the room so the corners are covered.
        Vector3 floorSize = new Vector3(size.x + thickness * 2f, 0.2f, size.z + thickness * 2f);
        Box("Floor", room, centre + new Vector3(0f, -0.1f, 0f), floorSize, floor);

        if (addCeiling == true)
        {
            Box("Ceiling", room, centre + new Vector3(0f, size.y + 0.1f, 0f), floorSize, ceiling);
        }

        // Back, left and right walls.
        float halfHeight = size.y / 2f;
        Box("Wall Back", room, centre + new Vector3(0f, halfHeight, size.z / 2f + thickness / 2f), new Vector3(size.x + thickness * 2f, size.y, thickness), wall);
        Box("Wall Left", room, centre + new Vector3(-size.x / 2f - thickness / 2f, halfHeight, 0f), new Vector3(thickness, size.y, size.z), wall);
        Box("Wall Right", room, centre + new Vector3(size.x / 2f + thickness / 2f, halfHeight, 0f), new Vector3(thickness, size.y, size.z), wall);

        // The front wall.
        float frontZ = -size.z / 2f - thickness / 2f;

        if (doorWidth <= 0f)
        {
            // No doorway: one solid wall.
            Box("Wall Front", room, centre + new Vector3(0f, halfHeight, frontZ), new Vector3(size.x + thickness * 2f, size.y, thickness), wall);
        }
        else
        {
            // A doorway: a piece of wall each side, and a lintel above the door.
            float sideWidth = (size.x - doorWidth) / 2f;
            float leftX = -size.x / 2f + sideWidth / 2f - thickness / 2f;
            float rightX = size.x / 2f - sideWidth / 2f + thickness / 2f;

            Box("Wall Front L", room, centre + new Vector3(leftX, halfHeight, frontZ), new Vector3(sideWidth + thickness, size.y, thickness), wall);
            Box("Wall Front R", room, centre + new Vector3(rightX, halfHeight, frontZ), new Vector3(sideWidth + thickness, size.y, thickness), wall);

            if (size.y > doorHeight)
            {
                float lintelY = (doorHeight + size.y) / 2f;
                Box("Door Lintel", room, centre + new Vector3(0f, lintelY, frontZ), new Vector3(doorWidth, size.y - doorHeight, thickness), wall);
            }
        }

        return room;
    }


    // =================================================================
    // MODELS AND EFFECTS
    // =================================================================

    // Place a model or prefab so it sits on the ground at "position".
    // targetHeight: scale it to this many metres tall (0 = keep its size).
    public static GameObject Model(string assetPath, Transform parent, Vector3 position, float yaw, float targetHeight)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null)
        {
            Debug.LogWarning("AreaKit: missing " + assetPath);
            return null;
        }

        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(source, ParentOrRoot(parent));

        // Keep the model's own import rotation and scale, then turn it by yaw.
        placed.transform.rotation = Quaternion.Euler(0f, yaw, 0f) * source.transform.localRotation;
        placed.transform.localScale = source.transform.localScale;
        placed.transform.position = position;

        // Scale it to the height we want.
        Bounds size = Bounds(placed);
        if (targetHeight > 0f && size.size.y > 0.0001f)
        {
            placed.transform.localScale = placed.transform.localScale * (targetHeight / size.size.y);
            size = Bounds(placed);
        }

        // Move it so its bottom-middle is exactly at "position".
        Vector3 nudge = new Vector3(position.x - size.center.x, position.y - size.min.y, position.z - size.center.z);
        placed.transform.position = placed.transform.position + nudge;

        return placed;
    }


    // Find a model file by its name (without .fbx). Returns its path, or null.
    public static string FindModel(string fileName)
    {
        string[] guids = AssetDatabase.FindAssets(fileName + " t:Model");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string nameOnly = System.IO.Path.GetFileNameWithoutExtension(path);

            // Skip .obj and .dae copies; we want the .fbx.
            if (nameOnly == fileName && path.EndsWith(".obj") == false && path.EndsWith(".dae") == false)
            {
                return path;
            }
        }

        return null;
    }


    // Place one of the Zoo's visual-effect prefabs.
    public static GameObject VFX(string prefabName, Transform parent, Vector3 position, float scale)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Zoo/VFX/" + prefabName + ".prefab");
        GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(source, ParentOrRoot(parent));
        placed.transform.position = position;
        placed.transform.localScale = Vector3.one * scale;
        return placed;
    }


    // Work out the box that fits around everything visible in an object.
    public static Bounds Bounds(GameObject target)
    {
        Renderer[] renderers = target.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.zero);
        }

        Bounds total = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            total.Encapsulate(renderers[i].bounds);
        }

        return total;
    }


    // =================================================================
    // LIGHTS
    // =================================================================

    // Turn true/false into soft shadows or no shadows.
    private static LightShadows ShadowSetting(bool shadows)
    {
        if (shadows == true)
        {
            return LightShadows.Soft;
        }

        return LightShadows.None;
    }


    // Add the sun (a Directional Light).
    public static Light Sun(Vector3 angles, Color colour, float intensity, bool shadows)
    {
        GameObject sunObject = new GameObject("Sun / Moon (Directional Light)");
        sunObject.transform.SetParent(Root, false);
        sunObject.transform.rotation = Quaternion.Euler(angles);

        Light sun = sunObject.AddComponent<Light>();
        sun.type = LightType.Directional;
        sun.color = colour;
        sun.intensity = intensity;
        sun.shadows = ShadowSetting(shadows);

        RenderSettings.sun = sun;
        return sun;
    }


    // Add a Point Light (shines in every direction, like a bulb).
    public static Light Point(string lightName, Transform parent, Vector3 position, Color colour, float range, float intensity, bool shadows)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(ParentOrRoot(parent), false);
        lightObject.transform.position = position;

        Light pointLight = lightObject.AddComponent<Light>();
        pointLight.type = LightType.Point;
        pointLight.color = colour;
        pointLight.range = range;
        pointLight.intensity = intensity;
        pointLight.shadows = ShadowSetting(shadows);

        return pointLight;
    }


    // Add a Spot Light (a cone of light, like a torch) pointing at "lookAt".
    public static Light Spot(string lightName, Transform parent, Vector3 position, Vector3 lookAt, Color colour, float range, float angle, float intensity, bool shadows)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(ParentOrRoot(parent), false);
        lightObject.transform.position = position;
        lightObject.transform.LookAt(lookAt);

        Light spotLight = lightObject.AddComponent<Light>();
        spotLight.type = LightType.Spot;
        spotLight.color = colour;
        spotLight.range = range;
        spotLight.spotAngle = angle;
        spotLight.intensity = intensity;
        spotLight.shadows = ShadowSetting(shadows);

        return spotLight;
    }


    // Set the ambient light colours and the skybox. Fog is switched off.
    // If skybox is null, the Lantern Grove sky is used.
    public static void Ambient(Color sky, Color equator, Color ground, Material skybox)
    {
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = sky;
        RenderSettings.ambientEquatorColor = equator;
        RenderSettings.ambientGroundColor = ground;

        if (skybox != null)
        {
            RenderSettings.skybox = skybox;
        }
        else
        {
            RenderSettings.skybox = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/LanternGrove/LG_Sky.mat");
        }

        RenderSettings.fog = false;
    }


    // =================================================================
    // TEXT, PLAQUES AND NOTICE BOARDS
    // =================================================================

    // Add 3D text in the world.
    public static TextMeshPro Text(Transform parent, string text, Vector3 position, float yaw, float size, Color colour, float width, float height, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(ParentOrRoot(parent), false);
        labelObject.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = size;
        label.color = colour;
        label.alignment = alignment;
        label.rectTransform.sizeDelta = new Vector2(width, height);
        label.textWrappingMode = TextWrappingModes.Normal;

        return label;
    }


    // A small sign on a post that explains an exhibit.
    public static void Plaque(Transform parent, string text, Vector3 position, float yaw)
    {
        Material plaqueMaterial = Mat("Area_Plaque", new Color(0.12f, 0.1f, 0.09f), 0.3f);

        Transform plaque = Group("Plaque", parent);
        plaque.position = position;
        plaque.rotation = Quaternion.Euler(0f, yaw, 0f);

        // The post.
        Prim(PrimitiveType.Cube, "Post", plaque, position + Vector3.up * 0.45f, new Vector3(0.08f, 0.9f, 0.08f), plaqueMaterial, false, yaw);

        // The sign, tilted back 25 degrees so it is easy to read.
        GameObject face = Prim(PrimitiveType.Cube, "Face", plaque, position + Vector3.up * 1.0f, new Vector3(1.1f, 0.6f, 0.05f), plaqueMaterial, false, yaw);
        face.transform.rotation = Quaternion.Euler(-25f, yaw, 0f);

        // The text, just in front of the sign.
        Vector3 inFront = Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.012f, -0.035f);
        TextMeshPro label = Text(plaque, text, position + Vector3.up * 1.0f + inFront, yaw, 0.55f, Color.white, 1.0f, 0.55f, TextAlignmentOptions.Center);
        label.transform.rotation = Quaternion.Euler(-25f, yaw, 0f);
    }


    // The area's notice board: what it teaches, and a list of "try this" tasks.
    public static void NoticeBoard(Vector3 position, float yaw, string title, string what, string[] tasks)
    {
        Material boardMaterial = Mat("Area_NoticeBoard", new Color(0.2f, 0.14f, 0.09f), 0.2f);

        Transform board = Group("Notice Board");
        Quaternion rotation = Quaternion.Euler(0f, yaw, 0f);
        board.SetPositionAndRotation(position, rotation);

        // The board and its two posts.
        Prim(PrimitiveType.Cube, "Panel", board, position + Vector3.up * 1.9f, new Vector3(3.2f, 2.3f, 0.1f), boardMaterial, true, yaw);
        Prim(PrimitiveType.Cube, "Post", board, position + rotation * new Vector3(-1.45f, 0.55f, 0.02f), new Vector3(0.12f, 1.1f, 0.12f), boardMaterial, true, yaw);
        Prim(PrimitiveType.Cube, "Post", board, position + rotation * new Vector3(1.45f, 0.55f, 0.02f), new Vector3(0.12f, 1.1f, 0.12f), boardMaterial, true, yaw);

        // Number the tasks: "1. ...", "2. ..."
        string taskList = "";
        for (int i = 0; i < tasks.Length; i++)
        {
            if (i > 0)
            {
                taskList = taskList + "\n";
            }
            taskList = taskList + (i + 1) + ". " + tasks[i];
        }

        // The words on the board. The <tags> are TextMeshPro formatting.
        string boardText = "<size=130%><b><color=#ffd970>" + title.ToUpper() + "</color></b></size>\n";
        boardText = boardText + what + "\n\n";
        boardText = boardText + "<b><color=#9fe3ff>TRY THIS</color></b>\n";
        boardText = boardText + "<size=92%>" + taskList + "</size>";

        Vector3 textPosition = position + Vector3.up * 1.9f + rotation * new Vector3(0f, 0f, -0.06f);
        Text(board, boardText, textPosition, yaw, 1.2f, Color.white, 3.0f, 2.15f, TextAlignmentOptions.TopLeft);
    }


    // =================================================================
    // THE PLAYER
    // =================================================================

    // Add the Starter Assets first-person player at "position", facing "yaw".
    public static Transform AddPlayer(Vector3 position, float yaw)
    {
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Starter Assets/Runtime/FirstPersonController/Prefabs/NestedParent_Unpack.prefab");
        GameObject rig = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(rig, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        rig.name = "Player (First Person)";

        Transform player = rig.transform.Find("PlayerCapsule");
        player.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

        Transform cameraRoot = player.Find("PlayerCameraRoot");
        Transform followCamera = rig.transform.Find("PlayerFollowCamera");

        // Set up the Cinemachine camera so it sits exactly at the player's eyes.
        // (SerializedObject lets us change Inspector values by their names.)
        Component[] cameraComponents = followCamera.GetComponents<Component>();

        for (int i = 0; i < cameraComponents.Length; i++)
        {
            Component component = cameraComponents[i];
            SerializedObject values = new SerializedObject(component);
            string componentName = component.GetType().Name;

            if (componentName == "CinemachineCamera")
            {
                values.FindProperty("Target.TrackingTarget").objectReferenceValue = cameraRoot;
            }

            if (componentName == "CinemachineThirdPersonFollow")
            {
                values.FindProperty("ShoulderOffset").vector3Value = Vector3.zero;
                values.FindProperty("VerticalArmLength").floatValue = 0f;
                values.FindProperty("CameraSide").floatValue = 0.5f;
                values.FindProperty("CameraDistance").floatValue = 0f;
                values.FindProperty("Damping").vector3Value = Vector3.zero;
                values.FindProperty("AvoidObstacles.Enabled").boolValue = false;
            }

            values.ApplyModifiedProperties();
        }

        // Start both cameras at the eyes too.
        Transform mainCamera = rig.transform.Find("MainCamera");
        mainCamera.SetPositionAndRotation(cameraRoot.position, cameraRoot.rotation);
        followCamera.SetPositionAndRotation(cameraRoot.position, cameraRoot.rotation);

        // Make sure post-processing (bloom etc.) is on for this camera.
        UnityEngine.Rendering.Universal.UniversalAdditionalCameraData cameraData = mainCamera.GetComponent<UnityEngine.Rendering.Universal.UniversalAdditionalCameraData>();
        if (cameraData != null)
        {
            cameraData.renderPostProcessing = true;
        }

        return player;
    }


    // =================================================================
    // THE ON-SCREEN UI
    // =================================================================

    // Make a UI object with its anchors, pivot, position and size.
    private static RectTransform UIObject(string objectName, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 position, Vector2 size)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        RectTransform rect = uiObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }


    // Make UI text that fills its parent (with a small margin).
    private static TextMeshProUGUI UIText(string objectName, Transform parent, string text, float size, TextAlignmentOptions alignment, Color colour)
    {
        RectTransform rect = UIObject(objectName, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        rect.offsetMin = new Vector2(20f, 10f);
        rect.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI label = rect.gameObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = alignment;
        label.color = colour;
        label.textWrappingMode = TextWrappingModes.Normal;
        return label;
    }


    // Add the Canvas with a MuseumUI: title, info panel, Press-E prompt and crosshair.
    //   showCounters  also add the "Found: 0 / 5" and "Gems: 0" box
    public static MuseumUI AddUI(string title, bool showCounters, string visitedLabel, int total)
    {
        // The Canvas.
        GameObject canvasObject = new GameObject("Area UI (Canvas)", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        MuseumUI ui = canvasObject.AddComponent<MuseumUI>();
        ui.totalWings = total;
        ui.visitedLabel = visitedLabel;

        Color darkPanel = new Color(0.08f, 0.07f, 0.06f, 0.8f);
        Vector2 middle = new Vector2(0.5f, 0.5f);

        // The title, top middle.
        RectTransform titleRect = UIObject("Title", canvasObject.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(1200f, 120f));
        ui.titleGroup = titleRect.gameObject.AddComponent<CanvasGroup>();
        ui.titleText = UIText("Title Text", titleRect, title, 66f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.62f));
        ui.titleText.fontStyle = FontStyles.Bold;

        // The info panel, bottom middle.
        RectTransform infoRect = UIObject("Info Panel", canvasObject.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1250f, 160f));
        infoRect.gameObject.AddComponent<Image>().color = darkPanel;
        ui.infoGroup = infoRect.gameObject.AddComponent<CanvasGroup>();
        ui.infoText = UIText("Info Text", infoRect, "", 29f, TextAlignmentOptions.Left, Color.white);

        // The "Press E" prompt, just below the middle.
        RectTransform promptRect = UIObject("Prompt", canvasObject.transform, middle, middle, middle, new Vector2(0f, -90f), new Vector2(620f, 70f));
        promptRect.gameObject.AddComponent<Image>().color = darkPanel;
        ui.promptGroup = promptRect.gameObject.AddComponent<CanvasGroup>();
        ui.promptText = UIText("Prompt Text", promptRect, "", 32f, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.6f));

        // The counters, top left.
        if (showCounters == true)
        {
            RectTransform countersRect = UIObject("Counters", canvasObject.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -30f), new Vector2(380f, 110f));
            countersRect.gameObject.AddComponent<Image>().color = darkPanel;

            RectTransform visitedRect = UIObject("Visited", countersRect, new Vector2(0f, 0.5f), new Vector2(1f, 1f), middle, Vector2.zero, Vector2.zero);
            ui.wingsText = UIText("Text", visitedRect, visitedLabel + ": 0 / " + total, 30f, TextAlignmentOptions.Left, Color.white);

            RectTransform gemsRect = UIObject("Gems", countersRect, new Vector2(0f, 0f), new Vector2(1f, 0.5f), middle, Vector2.zero, Vector2.zero);
            ui.gemsText = UIText("Text", gemsRect, "Gems: 0", 30f, TextAlignmentOptions.Left, new Color(0.55f, 0.95f, 1f));
        }

        // A small dot in the middle of the screen.
        RectTransform crosshair = UIObject("Crosshair", canvasObject.transform, middle, middle, middle, Vector2.zero, new Vector2(8f, 8f));
        crosshair.gameObject.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.7f);

        return ui;
    }


    // =================================================================
    // TRIGGER ZONES
    // =================================================================

    // Add an invisible box that notices when the player walks in.
    public static TriggerZone Zone(Transform parent, string zoneName, Vector3 centre, Vector3 size)
    {
        GameObject zoneObject = new GameObject(zoneName);
        zoneObject.transform.SetParent(ParentOrRoot(parent), false);
        zoneObject.transform.position = centre;

        BoxCollider box = zoneObject.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = size;

        return zoneObject.AddComponent<TriggerZone>();
    }


    // A trigger zone that shows a title and info text while the player is inside it.
    //   visitId  if not null, also counts as "visited" for the counters
    //
    // UnityEventTools adds the same kind of entries you would add by hand
    // with the + button on "On Player Enter" in the Inspector.
    public static TriggerZone InfoZone(MuseumUI ui, string title, string info, Vector3 centre, Vector3 size, string visitId)
    {
        TriggerZone zone = Zone(Group("Info Triggers"), "Info - " + title, centre, size);

        UnityEventTools.AddStringPersistentListener(zone.onPlayerEnter, ui.ShowTitle, title);
        UnityEventTools.AddStringPersistentListener(zone.onPlayerEnter, ui.ShowInfo, info);

        if (visitId != null)
        {
            UnityEventTools.AddStringPersistentListener(zone.onPlayerEnter, ui.VisitWing, visitId);
        }

        UnityEventTools.AddPersistentListener(zone.onPlayerExit, ui.HideInfo);

        return zone;
    }
}
