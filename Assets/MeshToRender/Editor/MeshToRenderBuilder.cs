using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

// =====================================================================
// MESH TO RENDER BUILDER  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT. You only need to USE it.
// It lives in an "Editor" folder, so it only runs inside the Unity
// Editor and is never part of your game.
//
// How to use it:
//   Tools > CRE134 > Build Mesh to Render Demo
//
// What it does:
//   Makes the scene "Assets/Scenes/Mesh to Render Demo.unity" with one
//   cube, a camera, a light and the MeshToRenderDemo script. It also
//   makes the four materials the demo swaps between, in
//   Assets/MeshToRender/Materials. Those materials are normal assets, so
//   you can also drag them onto a fresh cube yourself during a live demo.
//
//   Running it again rebuilds the scene and updates the materials.
// =====================================================================
public static class MeshToRenderBuilder
{
    const string Folder = "Assets/MeshToRender";
    const string MatFolder = Folder + "/Materials";
    const string TexFolder = Folder + "/Textures";
    const string ScenePath = "Assets/1. Scenes/1. For Teaching/Mesh to Render Demo.unity";
    const string NormalPreviewPath = TexFolder + "/Brick 1 Normals (picture).png";


    [MenuItem("Tools/CRE134/Build Mesh to Render Demo")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            bool rebuild = EditorUtility.DisplayDialog("Mesh to Render Demo",
                "The demo scene already exists. Rebuild it from scratch?\n\nAny changes you made to it (e.g. the skybox) will be lost.", "Rebuild", "Cancel");
            if (rebuild == false) return;
        }

        // Give the user the chance to save whatever scene is open now.
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
        {
            return;
        }

        // Find the brick textures by name, wherever they have been moved to.
        string BrickPath = FindTexture("Brick 1");
        string NormalPath = FindTexture("Brick 1 Normals");

        MakeFolder("Assets", "MeshToRender");
        MakeFolder(Folder, "Materials");
        MakeFolder(Folder, "Textures");

        // ---- Textures ----
        Texture2D brick = BrickPath == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(BrickPath);
        Texture2D normal = NormalPath == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPath);
        if (brick == null || normal == null)
        {
            EditorUtility.DisplayDialog("Mesh to Render Demo",
                "Couldn't find the textures 'Brick 1' and 'Brick 1 Normals' from the PBS Materials Variety Pack.", "OK");
            return;
        }

        // Make sure the normal map is imported as a Normal Map.
        TextureImporter normalImporter = (TextureImporter)AssetImporter.GetAtPath(NormalPath);
        if (normalImporter.textureType != TextureImporterType.NormalMap)
        {
            normalImporter.textureType = TextureImporterType.NormalMap;
            normalImporter.SaveAndReimport();
        }

        // A copy of the normal map imported as a plain picture, so it can be
        // SHOWN on screen as the blue/purple image (a real normal map is
        // stored in a special format that looks wrong if you display it).
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPreviewPath) == null)
        {
            AssetDatabase.CopyAsset(NormalPath, NormalPreviewPath);
            TextureImporter preview = (TextureImporter)AssetImporter.GetAtPath(NormalPreviewPath);
            preview.textureType = TextureImporterType.Default;
            preview.sRGBTexture = true;
            preview.mipmapEnabled = false;
            preview.maxTextureSize = 512;
            preview.SaveAndReimport();
        }
        Texture2D normalPicture = AssetDatabase.LoadAssetAtPath<Texture2D>(NormalPreviewPath);

        // ---- Materials ----
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");

        Material meshMat = MakeMat("1 Mesh - Plain Faces", lit);
        meshMat.SetColor("_BaseColor", new Color(0.86f, 0.89f, 0.95f));
        meshMat.SetFloat("_Smoothness", 0.1f);
        meshMat.SetFloat("_Metallic", 0f);

        Material textureMat = MakeMat("2 Texture - Unlit Base Map", unlit);
        textureMat.SetColor("_BaseColor", Color.white);
        textureMat.SetTexture("_BaseMap", brick);
        textureMat.SetTexture("_MainTex", brick);

        Material litMat = MakeMat("3 Material - Lit", lit);
        litMat.SetColor("_BaseColor", Color.white);
        litMat.SetTexture("_BaseMap", brick);
        litMat.SetTexture("_MainTex", brick);
        litMat.SetFloat("_Smoothness", 0.45f);
        litMat.SetFloat("_Metallic", 0f);
        litMat.SetTexture("_BumpMap", null);
        litMat.DisableKeyword("_NORMALMAP");

        Material normalMat = MakeMat("4 Normal Map - Lit + Normal", lit);
        normalMat.SetColor("_BaseColor", Color.white);
        normalMat.SetTexture("_BaseMap", brick);
        normalMat.SetTexture("_MainTex", brick);
        normalMat.SetFloat("_Smoothness", 0.45f);
        normalMat.SetFloat("_Metallic", 0f);
        normalMat.SetTexture("_BumpMap", normal);
        normalMat.SetFloat("_BumpScale", 1.2f);
        normalMat.EnableKeyword("_NORMALMAP");

        Material edgeMat = MakeMat("Mesh View - Edges", unlit);
        edgeMat.SetColor("_BaseColor", new Color(0.16f, 0.18f, 0.24f));

        Material diagonalMat = MakeMat("Mesh View - Triangle Splits", unlit);
        diagonalMat.SetColor("_BaseColor", new Color(0.23f, 0.49f, 0.85f));

        Material vertexMat = MakeMat("Mesh View - Vertices", unlit);
        vertexMat.SetColor("_BaseColor", new Color(0.85f, 0.33f, 0.31f));

        Material floorMat = MakeMat("Floor", lit);
        floorMat.SetColor("_BaseColor", new Color(0.40f, 0.42f, 0.48f));
        floorMat.SetFloat("_Smoothness", 0.15f);

        AssetDatabase.SaveAssets();

        // ---- Scene ----
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera, looking at the cube from a three-quarter view like the slide.
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera cam = cameraObject.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.93f, 0.94f, 0.96f);
        cam.fieldOfView = 30f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.transform.position = new Vector3(3.3f, 2.9f, -3.3f);
        cameraObject.transform.LookAt(new Vector3(0f, 0.25f, 0f));

        // Key light.
        GameObject lightObject = new GameObject("Key Light");
        Light keyLight = lightObject.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.intensity = 1.2f;
        keyLight.shadows = LightShadows.None;
        lightObject.transform.rotation = Quaternion.Euler(50f, 30f, 0f);

        // The cube: Unity's ordinary default cube.
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.position = new Vector3(0f, 0.5f, 0f);
        cube.GetComponent<MeshRenderer>().sharedMaterial = meshMat;

        // Floor, only shown in stage 5 to catch the shadow.
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor (stage 5 only)";
        floor.transform.localScale = new Vector3(4f, 1f, 4f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMat;
        floor.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;

        // Reads clicks on the stage buttons.
        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        // The demo script.
        GameObject demoObject = new GameObject("Mesh to Render Demo");
        MeshToRenderDemo demo = demoObject.AddComponent<MeshToRenderDemo>();
        demo.cube = cube.GetComponent<MeshRenderer>();
        demo.keyLight = keyLight;
        demo.cam = cam;
        demo.floor = floor;
        demo.meshMaterial = meshMat;
        demo.textureMaterial = textureMat;
        demo.litMaterial = litMat;
        demo.normalMaterial = normalMat;
        demo.edgeMaterial = edgeMat;
        demo.diagonalMaterial = diagonalMat;
        demo.vertexMaterial = vertexMat;
        demo.baseMapImage = brick;
        demo.normalMapImage = normalPicture;

        // Plain lighting: no skybox, flat ambient, no fog.
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = demo.neutralAmbient;
        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
        RenderSettings.fog = false;
        RenderSettings.sun = keyLight;

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = demoObject;

        EditorUtility.DisplayDialog("Mesh to Render Demo",
            "Built " + ScenePath + "\n\n" +
            "Press Play (Play Maximized looks best), then:\n" +
            "  1-5 or Left / Right   change stage\n" +
            "  Drag                  turn the cube\n" +
            "  Space                 spin the cube\n" +
            "  L                     move the light\n" +
            "  H                     hide the text", "OK");
    }


    // Make (or update) a material asset.
    static Material MakeMat(string materialName, Shader shader)
    {
        string path = MatFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
        }
        else
        {
            material.shader = shader;
        }

        EditorUtility.SetDirty(material);
        return material;
    }


    // Find a texture by its exact file name (no extension). Returns its path, or null.
    static string FindTexture(string textureName)
    {
        foreach (string guid in AssetDatabase.FindAssets(textureName + " t:Texture2D"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == textureName && path.StartsWith(Folder) == false)
            {
                return path;
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
