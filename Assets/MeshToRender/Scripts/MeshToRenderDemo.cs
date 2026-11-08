using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.UI;

// =====================================================================
// MESH TO RENDER DEMO
// ---------------------------------------------------------------------
// Builds one cube up in five layers, the same as the lecture slide:
//   1 Mesh       vertices, edges and faces (12 triangles)
//   2 Texture    a 2D image wrapped on with UVs, colour only (Unlit)
//   3 Material   URP Lit settings: how the surface reacts to light
//   4 Normal map fakes bumps per pixel, no extra polygons
//   5 Lighting   warm key light, cool ambient, cast shadow
//
// Controls in Play mode:
//   1-5, Left / Right, Page Up / Page Down   change stage
//                                            (a presentation clicker works)
//   Click a stage button                     jump to that stage
//   Drag with the mouse                      turn the cube
//   Space                                    spin the cube on / off
//   L                                        move the light on / off
//   H                                        hide / show the text
//
// The important idea: the cube's MESH never changes. Only the material
// on it and the lights around it change.
// =====================================================================
public class MeshToRenderDemo : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    [Header("Scene objects")]
    public MeshRenderer cube;
    public Light keyLight;
    public Camera cam;
    public GameObject floor;

    [Header("One material per stage")]
    public Material meshMaterial;      // 1  plain faces
    public Material textureMaterial;   // 2  URP Unlit + Base Map
    public Material litMaterial;       // 3  URP Lit + Base Map
    public Material normalMaterial;    // 4 and 5  URP Lit + Base Map + Normal Map

    [Header("Mesh view (stage 1)")]
    public Material edgeMaterial;      // dark lines
    public Material diagonalMaterial;  // blue lines
    public Material vertexMaterial;    // red dots

    [Header("Pictures shown beside the cube")]
    public Texture baseMapImage;       // the flat brick image
    public Texture normalMapImage;     // the normal map, as a picture

    [Header("Neutral light (stages 1-4)")]
    public Color neutralBackground = new Color(0.93f, 0.94f, 0.96f);
    public Color neutralAmbient = new Color(0.52f, 0.53f, 0.56f);
    public Color neutralLightColour = Color.white;
    public float neutralLightIntensity = 1.2f;
    public Vector3 neutralLightAngles = new Vector3(50f, 30f, 0f);

    [Header("Mood light (stage 5)")]
    public Color moodBackground = new Color(0.09f, 0.11f, 0.17f);
    public Color moodAmbient = new Color(0.20f, 0.26f, 0.42f);
    public Color moodLightColour = new Color(1f, 0.76f, 0.50f);
    public float moodLightIntensity = 1.8f;
    public Vector3 moodLightAngles = new Vector3(28f, 62f, 0f);

    [Header("Movement")]
    public float spinSpeed = 20f;      // degrees per second
    public float lightSpeed = 40f;     // degrees per second
    public float dragSpeed = 0.3f;

    [Range(1, 5)]
    public int startStage = 1;

    // ---------- Private variables ----------

    private int stage;
    private bool spinning;
    private bool lightMoving;

    private GameObject meshOverlay;
    private int vertexCount;
    private int edgeCount;
    private int triangleCount;

    private Canvas canvas;
    private Image[] chips = new Image[5];
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI subtitleText;
    private TextMeshProUGUI hintText;
    private TextMeshProUGUI descriptionText;
    private TextMeshProUGUI extraText;
    private GameObject insetRoot;
    private RawImage insetImage;
    private TextMeshProUGUI insetCaption;

    // The same colours and words as the slide.
    private readonly Color[] stageColours =
    {
        new Color(0.91f, 0.38f, 0.30f),   // red
        new Color(0.85f, 0.59f, 0.17f),   // amber
        new Color(0.23f, 0.62f, 0.49f),   // green
        new Color(0.48f, 0.36f, 0.65f),   // purple
        new Color(0.23f, 0.49f, 0.85f)    // blue
    };

    private readonly string[] stageNames = { "Mesh", "Texture", "Material", "Normal map", "Lighting" };

    private readonly string[] stageDescriptions =
    {
        "Vertices, edges and faces. Even a cube is 12 triangles.",
        "A 2D image wrapped onto the faces using UVs. Colour only.",
        "Shader settings: how the surface reacts to light.",
        "Fakes bumps by bending light per pixel. No extra polygons.",
        "Scene lights add direction, colour, highlights and shadow."
    };


    // =================================================================
    // START AND UPDATE
    // =================================================================

    void Start()
    {
        BuildMeshOverlay();
        BuildUI();
        SetStage(startStage);
    }


    void Update()
    {
        Keyboard keys = Keyboard.current;

        if (keys != null)
        {
            if (keys.digit1Key.wasPressedThisFrame || keys.numpad1Key.wasPressedThisFrame) SetStage(1);
            if (keys.digit2Key.wasPressedThisFrame || keys.numpad2Key.wasPressedThisFrame) SetStage(2);
            if (keys.digit3Key.wasPressedThisFrame || keys.numpad3Key.wasPressedThisFrame) SetStage(3);
            if (keys.digit4Key.wasPressedThisFrame || keys.numpad4Key.wasPressedThisFrame) SetStage(4);
            if (keys.digit5Key.wasPressedThisFrame || keys.numpad5Key.wasPressedThisFrame) SetStage(5);

            if (keys.rightArrowKey.wasPressedThisFrame || keys.pageDownKey.wasPressedThisFrame) SetStage(stage + 1);
            if (keys.leftArrowKey.wasPressedThisFrame || keys.pageUpKey.wasPressedThisFrame) SetStage(stage - 1);

            if (keys.spaceKey.wasPressedThisFrame) spinning = !spinning;
            if (keys.lKey.wasPressedThisFrame) lightMoving = !lightMoving;
            if (keys.hKey.wasPressedThisFrame) canvas.enabled = !canvas.enabled;
        }

        // Spin the cube.
        if (spinning)
        {
            cube.transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }

        // Drag with the mouse to turn the cube (but not when clicking the buttons).
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && PointerOverUI() == false)
        {
            float turn = -mouse.delta.ReadValue().x * dragSpeed;
            cube.transform.Rotate(Vector3.up, turn, Space.World);
        }

        // Move the light around the cube.
        if (lightMoving)
        {
            keyLight.transform.Rotate(Vector3.up, lightSpeed * Time.deltaTime, Space.World);
        }
    }


    // =================================================================
    // THE FIVE STAGES
    // =================================================================

    public void SetStage(int newStage)
    {
        stage = Mathf.Clamp(newStage, 1, 5);

        // ---- What is ON the cube ----
        // The mesh stays the same. Only the material changes.
        if (stage == 1) cube.sharedMaterial = meshMaterial;
        if (stage == 2) cube.sharedMaterial = textureMaterial;
        if (stage == 3) cube.sharedMaterial = litMaterial;
        if (stage >= 4) cube.sharedMaterial = normalMaterial;

        meshOverlay.SetActive(stage == 1);

        // ---- The lights AROUND the cube ----
        if (stage == 5)
        {
            SetLighting(moodBackground, moodAmbient, moodLightColour, moodLightIntensity, moodLightAngles, true);
        }
        else
        {
            SetLighting(neutralBackground, neutralAmbient, neutralLightColour, neutralLightIntensity, neutralLightAngles, false);
        }

        // The floor is only there to catch the shadow.
        floor.SetActive(stage == 5);

        UpdateUI();
    }


    void SetLighting(Color background, Color ambient, Color lightColour, float intensity, Vector3 angles, bool shadows)
    {
        cam.backgroundColor = background;

        // Ambient light: the soft light that comes from everywhere.
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = ambient;
        SphericalHarmonicsL2 probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(ambient);
        RenderSettings.ambientProbe = probe;

        // Key light: the main light, with a direction.
        keyLight.color = lightColour;
        keyLight.intensity = intensity;
        keyLight.transform.rotation = Quaternion.Euler(angles);

        if (shadows)
        {
            keyLight.shadows = LightShadows.Soft;
            keyLight.shadowStrength = 0.85f;
        }
        else
        {
            keyLight.shadows = LightShadows.None;
        }
    }


    // =================================================================
    // STAGE 1: DRAW THE REAL MESH
    // Reads the vertices and triangles from the cube's mesh and draws
    // a red dot on every corner and a line along every edge.
    // =================================================================

    void BuildMeshOverlay()
    {
        meshOverlay = new GameObject("Mesh View (made from the real mesh)");
        meshOverlay.transform.SetParent(cube.transform, false);

        Mesh mesh = cube.GetComponent<MeshFilter>().sharedMesh;
        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;
        triangleCount = triangles.Length / 3;

        // ---- Corners ----
        // Unity stores 24 vertices for a cube (each corner is copied once
        // per face so it can have its own normal and UV). We only want to
        // draw each corner once, so skip copies in the same place.
        List<Vector3> corners = new List<Vector3>();
        foreach (Vector3 v in vertices)
        {
            bool alreadyHave = false;
            foreach (Vector3 c in corners)
            {
                if (Vector3.Distance(c, v) < 0.0001f) alreadyHave = true;
            }
            if (alreadyHave == false) corners.Add(v);
        }
        vertexCount = corners.Count;

        foreach (Vector3 corner in corners)
        {
            GameObject dot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dot.name = "Vertex";
            Destroy(dot.GetComponent<Collider>());
            dot.transform.SetParent(meshOverlay.transform, false);
            dot.transform.localPosition = corner;
            dot.transform.localScale = Vector3.one * 0.05f;
            MeshRenderer dotRenderer = dot.GetComponent<MeshRenderer>();
            dotRenderer.sharedMaterial = vertexMaterial;
            dotRenderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---- Edges ----
        // Every triangle has 3 edges. An edge shared by two triangles that
        // face the SAME way is a diagonal across a square face (blue).
        // Any other edge is a real edge of the cube (dark).
        Dictionary<string, Edge> edges = new Dictionary<string, Edge>();
        for (int i = 0; i < triangles.Length; i += 3)
        {
            Vector3 a = vertices[triangles[i]];
            Vector3 b = vertices[triangles[i + 1]];
            Vector3 c = vertices[triangles[i + 2]];
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;

            AddEdge(edges, a, b, normal);
            AddEdge(edges, b, c, normal);
            AddEdge(edges, c, a, normal);
        }

        edgeCount = 0;
        foreach (Edge edge in edges.Values)
        {
            bool isDiagonal = Vector3.Dot(edge.normalA, edge.normalB) > 0.99f;
            if (isDiagonal == false) edgeCount++;

            // Lift the line very slightly off the surface so it isn't hidden.
            Vector3 lift = (edge.normalA + edge.normalB).normalized * 0.004f;

            GameObject lineObject = new GameObject(isDiagonal ? "Triangle split" : "Edge");
            lineObject.transform.SetParent(meshOverlay.transform, false);
            LineRenderer line = lineObject.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.SetPosition(0, edge.start + lift);
            line.SetPosition(1, edge.end + lift);
            line.widthMultiplier = isDiagonal ? 0.012f : 0.02f;
            line.numCapVertices = 2;
            line.sharedMaterial = isDiagonal ? diagonalMaterial : edgeMaterial;
            line.shadowCastingMode = ShadowCastingMode.Off;
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


    void AddEdge(Dictionary<string, Edge> edges, Vector3 a, Vector3 b, Vector3 normal)
    {
        // The same edge should get the same name whichever way round it is.
        string keyA = Key(a);
        string keyB = Key(b);
        string key = string.CompareOrdinal(keyA, keyB) < 0 ? keyA + "|" + keyB : keyB + "|" + keyA;

        if (edges.TryGetValue(key, out Edge existing))
        {
            existing.normalB = normal;
        }
        else
        {
            edges[key] = new Edge { start = a, end = b, normalA = normal, normalB = normal };
        }
    }


    string Key(Vector3 p)
    {
        return Mathf.RoundToInt(p.x * 1000) + "," + Mathf.RoundToInt(p.y * 1000) + "," + Mathf.RoundToInt(p.z * 1000);
    }


    // =================================================================
    // ON-SCREEN TEXT
    // =================================================================

    void UpdateUI()
    {
        int index = stage - 1;

        // Highlight the current stage button.
        for (int i = 0; i < 5; i++)
        {
            Color c = stageColours[i];
            c.a = (i == index) ? 1f : 0.35f;
            chips[i].color = c;
            chips[i].transform.localScale = (i == index) ? Vector3.one : Vector3.one * 0.92f;
        }

        descriptionText.text = "<b>" + stageNames[index] + "</b>   " + stageDescriptions[index];

        if (stage == 1)
        {
            extraText.text = "<color=#D9534F>" + vertexCount + " vertices</color> (red dots)   ·   "
                           + edgeCount + " edges (dark lines)   ·   "
                           + "<color=#3B7DD8>" + triangleCount + " triangles</color> (blue lines split each square in two)";
        }
        if (stage == 2)
        {
            extraText.text = "Shown <b>Unlit</b> on purpose: nothing reacts to light, so it looks flat and fake.";
        }
        if (stage == 3)
        {
            float smoothness = litMaterial.GetFloat("_Smoothness");
            float metallic = litMaterial.GetFloat("_Metallic");
            extraText.text = "URP Lit material   ·   Smoothness " + smoothness.ToString("0.00")
                           + "   ·   Metallic " + metallic.ToString("0.00")
                           + "   ·   look for the sheen on the top face";
        }
        if (stage == 4)
        {
            extraText.text = "Press <b>L</b> to move the light. The bricks look raised, but the cube's outline is still perfectly straight.";
        }
        if (stage == 5)
        {
            extraText.text = "<b>Key idea</b>   Mesh = the shape. Material (with its textures and maps) = the surface. Lighting reveals both.";
        }

        // The picture beside the cube.
        insetRoot.SetActive(stage == 2 || stage == 4);
        if (stage == 2)
        {
            insetImage.texture = baseMapImage;
            insetCaption.text = "2D image";
        }
        if (stage == 4)
        {
            insetImage.texture = normalMapImage;
            insetCaption.text = "normal map";
        }

        // Light text on the dark stage 5 background, dark text otherwise.
        Color textColour = (stage == 5) ? new Color(0.95f, 0.95f, 0.97f) : new Color(0.12f, 0.14f, 0.20f);
        Color softColour = (stage == 5) ? new Color(0.75f, 0.78f, 0.85f) : new Color(0.40f, 0.43f, 0.50f);
        titleText.color = textColour;
        subtitleText.color = softColour;
        hintText.color = softColour;
        insetCaption.color = softColour;
    }


    void BuildUI()
    {
        // Something has to read the mouse clicks on the buttons.
        if (FindAnyObjectByType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        GameObject canvasObject = new GameObject("Demo UI");
        canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        Transform root = canvasObject.transform;

        // ---- Title, top left ----
        titleText = MakeText("Title", root, "From mesh to final render", 56, FontStyles.Bold, TextAlignmentOptions.TopLeft);
        Place(titleText.rectTransform, new Vector2(0, 1), new Vector2(60, -40), new Vector2(1200, 80));

        subtitleText = MakeText("Subtitle", root, "Five layers that turn a plain box into a believable game object", 28, FontStyles.Italic, TextAlignmentOptions.TopLeft);
        Place(subtitleText.rectTransform, new Vector2(0, 1), new Vector2(60, -118), new Vector2(1200, 50));

        // ---- Controls, top right ----
        hintText = MakeText("Controls", root,
            "<b>1-5</b> or <b>Left / Right</b>  stage\n<b>Drag</b>  turn cube    <b>Space</b>  spin\n<b>L</b>  move light    <b>H</b>  hide text",
            22, FontStyles.Normal, TextAlignmentOptions.TopRight);
        Place(hintText.rectTransform, new Vector2(1, 1), new Vector2(-60, -44), new Vector2(520, 110));

        // ---- Panel along the bottom ----
        Image panel = MakeImage("Bottom Panel", root, new Color(1f, 1f, 1f, 0.92f));
        Place(panel.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 40), new Vector2(1800, 230));

        for (int i = 0; i < 5; i++)
        {
            int stageNumber = i + 1;
            Image chip = MakeImage("Stage " + stageNumber, panel.transform, stageColours[i]);
            Place(chip.rectTransform, new Vector2(0.5f, 1), new Vector2(-720 + i * 360, -24), new Vector2(330, 64));
            chip.rectTransform.pivot = new Vector2(0.5f, 1);

            Button button = chip.gameObject.AddComponent<Button>();
            button.onClick.AddListener(() => SetStage(stageNumber));

            TextMeshProUGUI label = MakeText("Label", chip.transform, stageNumber + "   " + stageNames[i], 30, FontStyles.Bold, TextAlignmentOptions.Center);
            label.color = Color.white;
            Stretch(label.rectTransform);
            chips[i] = chip;
        }

        descriptionText = MakeText("Description", panel.transform, "", 36, FontStyles.Normal, TextAlignmentOptions.Center);
        descriptionText.color = new Color(0.12f, 0.14f, 0.20f);
        Place(descriptionText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -108), new Vector2(1720, 54));
        descriptionText.rectTransform.pivot = new Vector2(0.5f, 1);

        extraText = MakeText("Extra", panel.transform, "", 26, FontStyles.Normal, TextAlignmentOptions.Center);
        extraText.color = new Color(0.35f, 0.38f, 0.46f);
        Place(extraText.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -168), new Vector2(1720, 44));
        extraText.rectTransform.pivot = new Vector2(0.5f, 1);

        // ---- The picture beside the cube ----
        Image frame = MakeImage("Inset", root, Color.white);
        Place(frame.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(390, 60), new Vector2(236, 236));
        frame.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        insetRoot = frame.gameObject;

        GameObject imageObject = new GameObject("Picture", typeof(RectTransform));
        imageObject.transform.SetParent(frame.transform, false);
        insetImage = imageObject.AddComponent<RawImage>();
        Stretch(insetImage.rectTransform);
        insetImage.rectTransform.offsetMin = new Vector2(8, 8);
        insetImage.rectTransform.offsetMax = new Vector2(-8, -8);

        insetCaption = MakeText("Caption", frame.transform, "", 24, FontStyles.Italic, TextAlignmentOptions.Top);
        Place(insetCaption.rectTransform, new Vector2(0.5f, 0), new Vector2(0, -8), new Vector2(236, 36));
        insetCaption.rectTransform.pivot = new Vector2(0.5f, 1);
    }


    // ---------- Small UI helpers ----------

    TextMeshProUGUI MakeText(string objectName, Transform parent, string text, float size, FontStyles style, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = size;
        tmp.fontStyle = style;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }


    Image MakeImage(string objectName, Transform parent, Color colour)
    {
        GameObject imageObject = new GameObject(objectName, typeof(RectTransform));
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = colour;
        return image;
    }


    // Anchor at one point of the parent, then move and size from there.
    void Place(RectTransform rect, Vector2 anchor, Vector2 position, Vector2 size)
    {
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }


    // Fill the whole parent.
    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }


    bool PointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
