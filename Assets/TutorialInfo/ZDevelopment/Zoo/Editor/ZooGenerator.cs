using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

// =====================================================================
// ZOO GENERATOR  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT. You only need to USE it.
// It lives in an "Editor" folder, so it only runs inside the Unity
// Editor and is never part of your game.
//
// What it does:
//   Lays out every model and prefab in a folder at its REAL size, in
//   neat rows, grouped by name, with a name + size label under each one.
//   Animated models also get a row showing each of their animations.
//
// How to use it:
//   In the Project window, right-click a folder of models and choose
//   Lantern Tools > Generate Zoo From Folder.
// =====================================================================
public static class ZooGenerator
{
    // ---------- Settings ----------

    // How wide a row can get (in metres) before starting a new row.
    private const float MaxRowWidth = 40f;

    // The gap between models (in metres).
    private const float Spacing = 1f;

    // Put models with similar names (Rock_1, Rock_2...) in their own rows?
    private static bool groupByName = true;

    // Add a name + size label under each model?
    private static bool addLabels = true;

    // Add a row of animations for animated models?
    private static bool addAnimationRows = true;

    // The most animations to show for one model.
    private const int MaxClipsPerModel = 20;

    // Turn models round so they face you as you walk up to the row?
    private static bool faceViewer = true;


    // =================================================================
    // THE MENU ITEM
    // =================================================================

    // This first method decides whether the menu item can be clicked:
    // only when at least one FOLDER is selected.
    [MenuItem("Assets/Lantern Tools/Generate Zoo From Folder", true)]
    private static bool CanGenerate()
    {
        string[] selected = Selection.assetGUIDs;

        for (int i = 0; i < selected.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(selected[i]);
            if (AssetDatabase.IsValidFolder(path))
            {
                return true;
            }
        }

        return false;
    }


    // This runs when you click the menu item.
    [MenuItem("Assets/Lantern Tools/Generate Zoo From Folder")]
    private static void GenerateFromSelection()
    {
        // Start after any zoos that are already in the scene.
        float z = FindNextFreeZ();

        string[] selected = Selection.assetGUIDs;

        for (int i = 0; i < selected.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(selected[i]);

            if (AssetDatabase.IsValidFolder(path) == false)
            {
                continue;
            }

            GameObject zoo = Generate(path, new Vector3(0f, 0f, z));

            // The next zoo goes 6 metres after this one.
            if (zoo != null)
            {
                z = Bounds(zoo).max.z + 6f;
            }
        }
    }


    // Find the far edge of any zoos already in the scene, so a new one doesn't overlap.
    private static float FindNextFreeZ()
    {
        float z = 0f;
        GameObject[] rootObjects = SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            if (rootObjects[i].name.StartsWith("Zoo - "))
            {
                float farEdge = Bounds(rootObjects[i]).max.z + 6f;
                z = Mathf.Max(z, farEdge);
            }
        }

        return z;
    }


    // =================================================================
    // BUILDING THE ZOO
    // =================================================================

    // Build a zoo from every model in "folder", starting at "origin".
    public static GameObject Generate(string folder, Vector3 origin)
    {
        // 1. Find the models, one file per name, sorted A to Z.
        List<string> paths = FindModelPaths(folder);

        if (paths.Count == 0)
        {
            Debug.LogWarning("Zoo: no models or prefabs found in " + folder);
            return null;
        }

        // 2. Make the top object that everything goes inside.
        GameObject zoo = new GameObject("Zoo - " + Path.GetFileName(folder));
        Undo.RegisterCreatedObjectUndo(zoo, "Generate Zoo");
        zoo.transform.position = origin;

        // 3. Sort the models into groups (one row or more per group).
        List<string> groupNames = new List<string>();
        List<List<string>> groupPaths = new List<List<string>>();
        MakeGroups(paths, groupNames, groupPaths);

        // 4. Lay out each group.
        float z = origin.z;
        List<GameObject> animatedModels = new List<GameObject>();

        for (int g = 0; g < groupNames.Count; g++)
        {
            GameObject groupObject = new GameObject(groupNames[g]);
            groupObject.transform.SetParent(zoo.transform, false);

            // The group's name, to the left of its row.
            if (addLabels == true && groupNames.Count > 1)
            {
                Label(groupObject.transform, "<b>" + groupNames[g] + "</b>", new Vector3(origin.x - 1.5f, 0.02f, z + 0.5f), 4f, 6f, TextAlignmentOptions.Right);
            }

            float x = origin.x;
            float rowDepth = 0f;   // how deep the deepest model in this row is

            for (int i = 0; i < groupPaths[g].Count; i++)
            {
                string path = groupPaths[g][i];

                GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (source == null)
                {
                    continue;
                }

                GameObject model = PlaceModel(source, groupObject.transform);
                Bounds size = Bounds(model);

                // Nothing visible? Remove it and move on.
                if (size.size == Vector3.zero)
                {
                    Object.DestroyImmediate(model);
                    continue;
                }

                // If this model would make the row too wide, start a new row.
                if (x > origin.x && x + size.size.x > origin.x + MaxRowWidth)
                {
                    x = origin.x;
                    z = z + rowDepth + 2f;
                    rowDepth = 0f;
                }

                // Move it so it sits on the floor, its left edge at x and its front edge at z.
                MoveToSpot(model, size, x, z);

                if (addLabels == true)
                {
                    string sizeText = size.size.x.ToString("0.0") + " x " + size.size.y.ToString("0.0") + " x " + size.size.z.ToString("0.0") + " m";
                    string labelText = source.name + "\n<size=70%>" + sizeText + "</size>";
                    Vector3 labelPosition = new Vector3(x + size.size.x / 2f, 0.02f, z - 0.6f);
                    float labelWidth = Mathf.Max(1.6f, size.size.x + 0.5f);
                    Label(groupObject.transform, labelText, labelPosition, 1.6f, labelWidth, TextAlignmentOptions.Center);
                }

                // Move along for the next model.
                x = x + size.size.x + Spacing;
                rowDepth = Mathf.Max(rowDepth, size.size.z);

                // Remember models that have animations.
                if (addAnimationRows == true && GetClips(path).Count > 0)
                {
                    animatedModels.Add(source);
                }
            }

            // Leave a gap before the next group.
            z = z + rowDepth + 3f;
        }

        // 5. One row for each animated model, with one copy per animation.
        for (int m = 0; m < animatedModels.Count; m++)
        {
            z = AddAnimationRow(animatedModels[m], zoo.transform, origin.x, z);
        }

        Selection.activeGameObject = zoo;
        return zoo;
    }


    // Add a row showing every animation of one model. Returns the z for the next row.
    private static float AddAnimationRow(GameObject source, Transform zoo, float startX, float z)
    {
        List<AnimationClip> clips = GetClips(AssetDatabase.GetAssetPath(source));

        GameObject rowObject = new GameObject(source.name + " - Animations");
        rowObject.transform.SetParent(zoo, false);

        if (addLabels == true)
        {
            Label(rowObject.transform, "<b>" + source.name + "</b>\n<size=60%>animations</size>", new Vector3(startX - 1.5f, 0.02f, z + 0.5f), 3f, 6f, TextAlignmentOptions.Right);
        }

        float x = startX;
        float rowDepth = 0f;

        for (int i = 0; i < clips.Count && i < MaxClipsPerModel; i++)
        {
            AnimationClip clip = clips[i];

            GameObject model = PlaceModel(source, rowObject.transform);
            Bounds size = Bounds(model);
            MoveToSpot(model, size, x, z);

            // It needs an Animator for ZooClipPlayer to work.
            Animator animator = model.GetComponent<Animator>();
            if (animator == null)
            {
                animator = model.AddComponent<Animator>();
            }
            animator.applyRootMotion = false;   // stay in place, don't walk off

            ZooClipPlayer player = model.AddComponent<ZooClipPlayer>();
            player.clip = clip;

            if (addLabels == true)
            {
                // Some clip names look like "Armature|Walk". Just show "Walk".
                string shortName = clip.name;
                if (clip.name.Contains("|"))
                {
                    shortName = clip.name.Substring(clip.name.LastIndexOf('|') + 1);
                }

                string details = clip.length.ToString("0.0") + " s";
                if (clip.isLooping)
                {
                    details = details + " loop";
                }

                Vector3 labelPosition = new Vector3(x + size.size.x / 2f, 0.02f, z - 0.6f);
                float labelWidth = Mathf.Max(1.6f, size.size.x + 0.5f);
                Label(rowObject.transform, shortName + "\n<size=70%>" + details + "</size>", labelPosition, 1.6f, labelWidth, TextAlignmentOptions.Center);
            }

            x = x + size.size.x + Spacing;
            rowDepth = Mathf.Max(rowDepth, size.size.z);
        }

        return z + rowDepth + 3f;
    }


    // Put a copy of a model in the scene (turned to face the viewer if wanted).
    private static GameObject PlaceModel(GameObject source, Transform parent)
    {
        GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source, parent);

        if (faceViewer == true)
        {
            model.transform.rotation = Quaternion.Euler(0f, 180f, 0f) * model.transform.rotation;
        }

        return model;
    }


    // Move a model so it sits on the floor, its left edge at x and its front edge at z.
    private static void MoveToSpot(GameObject model, Bounds size, float x, float z)
    {
        Vector3 move = new Vector3(x - size.min.x, -size.min.y, z - size.min.z);
        model.transform.position = model.transform.position + move;
    }


    // =================================================================
    // FINDING AND GROUPING THE MODELS
    // =================================================================

    // Find every model and prefab in the folder.
    // If the same name exists as more than one file (e.g. Tree.fbx and Tree.prefab),
    // keep only the best one: prefab first, then fbx, glb, gltf, obj, dae, blend.
    private static List<string> FindModelPaths(string folder)
    {
        string[] preferredOrder = { ".prefab", ".fbx", ".glb", ".gltf", ".obj", ".dae", ".blend" };

        // For each name, the best path found so far.
        Dictionary<string, string> bestPathForName = new Dictionary<string, string>();
        List<string> names = new List<string>();

        string[] guids = AssetDatabase.FindAssets("t:Model t:Prefab", new string[] { folder });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            string modelName = Path.GetFileNameWithoutExtension(path);

            if (bestPathForName.ContainsKey(modelName) == false)
            {
                bestPathForName.Add(modelName, path);
                names.Add(modelName);
            }
            else
            {
                // Is this file type better than the one we already have?
                int newRank = FileTypeRank(path, preferredOrder);
                int oldRank = FileTypeRank(bestPathForName[modelName], preferredOrder);

                if (newRank < oldRank)
                {
                    bestPathForName[modelName] = path;
                }
            }
        }

        // Sort the names A to Z, then build the list of paths in that order.
        names.Sort();

        List<string> paths = new List<string>();
        for (int i = 0; i < names.Count; i++)
        {
            paths.Add(bestPathForName[names[i]]);
        }

        return paths;
    }


    // Where a file's type is in the preferred list (0 = best). Unknown types get 99.
    private static int FileTypeRank(string path, string[] preferredOrder)
    {
        string extension = Path.GetExtension(path).ToLower();

        for (int i = 0; i < preferredOrder.Length; i++)
        {
            if (preferredOrder[i] == extension)
            {
                return i;
            }
        }

        return 99;
    }


    // Sort the paths into groups by name. Fills in groupNames and groupPaths.
    private static void MakeGroups(List<string> paths, List<string> groupNames, List<List<string>> groupPaths)
    {
        for (int i = 0; i < paths.Count; i++)
        {
            string groupName = "All";
            if (groupByName == true)
            {
                groupName = GroupName(Path.GetFileNameWithoutExtension(paths[i]));
            }

            // Find this group, or make it.
            int groupIndex = groupNames.IndexOf(groupName);
            if (groupIndex == -1)
            {
                groupNames.Add(groupName);
                groupPaths.Add(new List<string>());
                groupIndex = groupNames.Count - 1;
            }

            groupPaths[groupIndex].Add(paths[i]);
        }

        // If most groups only hold ONE model (Alpaca, Bull, Cow...), grouping
        // just wastes space. Put everything in one group instead.
        int singleGroups = 0;
        for (int g = 0; g < groupPaths.Count; g++)
        {
            if (groupPaths[g].Count == 1)
            {
                singleGroups = singleGroups + 1;
            }
        }

        if (groupNames.Count > 1 && singleGroups > groupNames.Count / 2)
        {
            groupNames.Clear();
            groupPaths.Clear();
            groupNames.Add("All");
            groupPaths.Add(new List<string>(paths));
        }
    }


    // Work out a group name from a model name:
    //   BirchTree_Autumn_3  ->  BirchTree   (the part before the first _)
    //   bathroomCabinet     ->  Bathroom    (the part before the next capital letter)
    public static string GroupName(string modelName)
    {
        if (modelName.Contains("_"))
        {
            string[] parts = modelName.Split('_');
            return parts[0];
        }

        // Go along the name until we reach a capital letter or a number.
        int end = 1;
        while (end < modelName.Length && char.IsUpper(modelName[end]) == false && char.IsDigit(modelName[end]) == false)
        {
            end = end + 1;
        }

        string groupName = modelName.Substring(0, end);

        // Make the first letter a capital.
        return char.ToUpper(groupName[0]) + groupName.Substring(1);
    }


    // Get every animation clip inside a model file, sorted A to Z.
    public static List<AnimationClip> GetClips(string path)
    {
        List<AnimationClip> clips = new List<AnimationClip>();
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);

        for (int i = 0; i < assets.Length; i++)
        {
            AnimationClip clip = assets[i] as AnimationClip;

            // Skip non-clips, and Unity's hidden "__preview" clips.
            if (clip != null && clip.name.StartsWith("__preview") == false)
            {
                clips.Add(clip);
            }
        }

        // Sort A to Z by name (a simple "insertion sort").
        for (int i = 1; i < clips.Count; i++)
        {
            AnimationClip current = clips[i];
            int j = i - 1;

            while (j >= 0 && string.Compare(clips[j].name, current.name) > 0)
            {
                clips[j + 1] = clips[j];
                j = j - 1;
            }

            clips[j + 1] = current;
        }

        return clips;
    }


    // =================================================================
    // SMALL HELPERS
    // =================================================================

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


    // Add a label lying flat on the floor, readable from the front.
    public static TextMeshPro Label(Transform parent, string text, Vector3 position, float size, float width, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject("Label");
        labelObject.transform.SetParent(parent, false);
        labelObject.transform.position = position;
        labelObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);   // lying flat

        TextMeshPro label = labelObject.AddComponent<TextMeshPro>();
        label.text = text;
        label.fontSize = size * 1.5f;
        label.fontStyle = FontStyles.Bold;
        label.alignment = alignment;
        label.color = new Color(0.04f, 0.04f, 0.07f);
        label.rectTransform.sizeDelta = new Vector2(width * 1.3f, 1.7f);
        label.textWrappingMode = TextWrappingModes.Normal;

        return label;
    }
}
