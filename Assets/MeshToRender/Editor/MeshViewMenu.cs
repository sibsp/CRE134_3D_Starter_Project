using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// =====================================================================
// MESH VIEW MENU  (an EDITOR tool)
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT. You only need to USE it.
//
// How to use it:
//   Select an object with a mesh (e.g. a cube), then
//   GameObject > CRE134 > Add Mesh View
//   (also on the right-click menu in the Hierarchy)
//
// It adds a child called "Mesh View" that shows the vertices, edges and
// triangles. It is saved in the scene, so it stays there. Untick it to
// hide it, tick it to show it again.
// =====================================================================
public static class MeshViewMenu
{
    const string MatFolder = "Assets/MeshToRender/Materials";


    [MenuItem("GameObject/CRE134/Add Mesh View", false, 10)]
    static void AddMeshView()
    {
        Material edgeMat = GetMat("Mesh View - Edges", new Color(0.16f, 0.18f, 0.24f));
        Material diagonalMat = GetMat("Mesh View - Triangle Splits", new Color(0.23f, 0.49f, 0.85f));
        Material vertexMat = GetMat("Mesh View - Vertices", new Color(0.85f, 0.33f, 0.31f));

        foreach (GameObject target in Selection.gameObjects)
        {
            if (target.GetComponent<MeshFilter>() == null) continue;

            // Already has one? Just rebuild it.
            MeshViewOverlay existing = target.GetComponentInChildren<MeshViewOverlay>(true);
            if (existing != null)
            {
                existing.Build();
                continue;
            }

            GameObject view = new GameObject("Mesh View");
            Undo.RegisterCreatedObjectUndo(view, "Add Mesh View");
            view.transform.SetParent(target.transform, false);

            MeshViewOverlay overlay = view.AddComponent<MeshViewOverlay>();
            overlay.edgeMaterial = edgeMat;
            overlay.diagonalMaterial = diagonalMat;
            overlay.vertexMaterial = vertexMat;
            overlay.Build();

            EditorSceneManager.MarkSceneDirty(target.scene);
        }
    }


    // Only allow the menu item when something with a mesh is selected.
    [MenuItem("GameObject/CRE134/Add Mesh View", true)]
    static bool AddMeshViewValid()
    {
        foreach (GameObject target in Selection.gameObjects)
        {
            if (target.GetComponent<MeshFilter>() != null) return true;
        }
        return false;
    }


    // Use the material from the demo if it exists, otherwise make it.
    static Material GetMat(string materialName, Color colour)
    {
        string path = MatFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        if (AssetDatabase.IsValidFolder("Assets/MeshToRender") == false) AssetDatabase.CreateFolder("Assets", "MeshToRender");
        if (AssetDatabase.IsValidFolder(MatFolder) == false) AssetDatabase.CreateFolder("Assets/MeshToRender", "Materials");

        material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        material.SetColor("_BaseColor", colour);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }
}
