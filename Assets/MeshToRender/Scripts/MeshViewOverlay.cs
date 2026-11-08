using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// =====================================================================
// MESH VIEW OVERLAY
// ---------------------------------------------------------------------
// Draws the parts of a mesh on top of an object, so you can SEE them:
//   red dots     the vertices (corners)
//   dark lines   the edges
//   blue lines   where a square face is split into two triangles
//
// How to use it:
//   Select a cube, then  GameObject > CRE134 > Add Mesh View
//   It appears as a child object called "Mesh View". Tick it on or off
//   in the Hierarchy / Inspector to show or hide it.
//
//   If you change the mesh, right-click this component's title in the
//   Inspector and choose "Rebuild".
//
// It reads the REAL mesh data (mesh.vertices and mesh.triangles), so
// the counts below are what Unity actually stores.
// =====================================================================
public class MeshViewOverlay : MonoBehaviour
{
    [Header("Materials")]
    public Material edgeMaterial;
    public Material diagonalMaterial;
    public Material vertexMaterial;

    [Header("Sizes")]
    public float dotSize = 0.05f;
    public float edgeWidth = 0.02f;
    public float diagonalWidth = 0.012f;

    [Header("What the mesh is made of (filled in by Rebuild)")]
    public int vertices;
    public int edges;
    public int triangles;

    // Meshes bigger than this would make far too many objects.
    private const int MaxTriangles = 2000;


    // Rebuild the dots and lines from the parent's mesh.
    [ContextMenu("Rebuild")]
    public void Build()
    {
        // Remove the old dots and lines first.
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            RemoveObject(transform.GetChild(i).gameObject);
        }

        MeshFilter meshFilter = transform.parent != null ? transform.parent.GetComponent<MeshFilter>() : null;
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("Mesh View needs to be a child of an object with a Mesh Filter.", this);
            return;
        }

        Mesh mesh = meshFilter.sharedMesh;
        Vector3[] meshVertices = mesh.vertices;
        int[] meshTriangles = mesh.triangles;
        triangles = meshTriangles.Length / 3;

        if (triangles > MaxTriangles)
        {
            Debug.LogWarning("Mesh View: this mesh has " + triangles + " triangles, which is too many to draw this way. Try it on a simple shape like a cube.", this);
            return;
        }

        // Sit exactly on the parent.
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;

        // ---- Vertices ----
        // Unity stores 24 vertices for a cube (each corner is copied once
        // per face so it can have its own normal and UV). We only want to
        // draw each corner once, so skip copies in the same place.
        List<Vector3> corners = new List<Vector3>();
        foreach (Vector3 v in meshVertices)
        {
            bool alreadyHave = false;
            foreach (Vector3 c in corners)
            {
                if (Vector3.Distance(c, v) < 0.0001f) alreadyHave = true;
            }
            if (alreadyHave == false) corners.Add(v);
        }
        vertices = corners.Count;

        // Keep the dots round even if the object is stretched.
        Vector3 scale = transform.lossyScale;
        Vector3 dotScale = new Vector3(dotSize / scale.x, dotSize / scale.y, dotSize / scale.z);

        foreach (Vector3 corner in corners)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Vertex";
            RemoveObject(dot.GetComponent<Collider>());
            dot.transform.SetParent(transform, false);
            dot.transform.localPosition = corner;
            dot.transform.localScale = dotScale;
            MeshRenderer dotRenderer = dot.GetComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = vertexMaterial;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---- Edges ----
        // Every triangle has 3 edges. An edge shared by two triangles that
        // face the SAME way is a split across a flat face (blue).
        // Any other edge is a real edge of the shape (dark).
        Dictionary<string, Edge> allEdges = new Dictionary<string, Edge>();
        for (int i = 0; i < meshTriangles.Length; i += 3)
        {
            Vector3 a = meshVertices[meshTriangles[i]];
            Vector3 b = meshVertices[meshTriangles[i + 1]];
            Vector3 c = meshVertices[meshTriangles[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

            AddEdge(allEdges, a, b, normal);
            AddEdge(allEdges, b, c, normal);
            AddEdge(allEdges, c, a, normal);
        }

        edges = 0;
        foreach (Edge edge in allEdges.Values)
        {
            bool isDiagonal = Vector3.Dot(edge.normalA, edge.normalB) > 0.99f;
            if (isDiagonal == false) edges++;

            // Lift the line very slightly off the surface so it isn't hidden.
            Vector3 lift = (edge.normalA + edge.normalB).normalized * 0.004f;

            GameObject lineObject = new GameObject(isDiagonal ? "Triangle split" : "Edge");
            lineObject.transform.SetParent(transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, edge.start + lift);
            line.SetPosition(1, edge.end + lift);
            line.widthMultiplier = isDiagonal ? diagonalWidth : edgeWidth;
            line.numCapVertices = 2;
            line.sharedMaterial = isDiagonal ? diagonalMaterial : edgeMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }


    // One edge of the mesh, and the facing direction of the triangles either side.
    private class Edge
    {
        public Vector3 start;
        public Vector3 end;
        public Vector3 normalA;
        public Vector3 normalB;
    }


    void AddEdge(Dictionary<string, Edge> allEdges, Vector3 a, Vector3 b, Vector3 normal)
    {
        // The same edge should get the same name whichever way round it is.
        string keyA = Key(a);
        string keyB = Key(b);
        string key = string.CompareOrdinal(keyA, keyB) < 0 ? keyA + "|" + keyB : keyB + "|" + keyA;

        if (allEdges.TryGetValue(key, out Edge existing))
        {
            existing.normalB = normal;
        }
        else
        {
            allEdges[key] = new Edge { start = a, end = b, normalA = normal, normalB = normal };
        }
    }


    string Key(Vector3 p)
    {
        return Mathf.RoundToInt(p.x * 1000) + "," + Mathf.RoundToInt(p.y * 1000) + "," + Mathf.RoundToInt(p.z * 1000);
    }


    // Destroy works in Play mode, DestroyImmediate is needed in the Editor.
    void RemoveObject(Object target)
    {
        if (Application.isPlaying)
        {
            Destroy(target);
        }
        else
        {
            DestroyImmediate(target);
        }
    }
}
