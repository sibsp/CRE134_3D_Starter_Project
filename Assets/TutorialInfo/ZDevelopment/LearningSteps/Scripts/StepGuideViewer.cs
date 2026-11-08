using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// =====================================================================
// STEP GUIDE VIEWER
// ---------------------------------------------------------------------
// YOU DON'T NEED TO UNDERSTAND THIS SCRIPT.
// It is the tool that shows the step guide on screen. It is longer than
// the other scripts because it builds all of its UI in code. Feel free
// to read it, but the other scripts are the ones to learn from first.
//
// What it does:
//   Press G in Play mode to open this step's guide.
//     Left / Right (or A / D)        change page
//     Up / Down (or the mouse wheel) scroll
//     G or Esc                       close
//   The player is frozen while the guide is open.
//
// How the guide text is written:
//   The guide is a text file (a TextAsset) using a few simple markers:
//     =PAGE Title            starts a new page
//     =SUB some text         a subtitle
//     =H3 some text          a sub-heading
//     =IMG name|caption      a picture from the Images list
//     =CODE caption          a block of code, ending with a line =END
//     =BOX #colour|Title     a coloured tip box, ending with a line =END
//   Any other line is normal text. A blank line starts a new paragraph.
// =====================================================================
public class StepGuideViewer : MonoBehaviour
{
    // ---------- Settings you can change in the Inspector ----------

    [Header("Content")]
    // The guide text file.
    public TextAsset guide;

    // Shown at the top of the guide, e.g. "STEP 01".
    public string stepLabel = "STEP 01";

    // The pictures the guide uses. The =IMG marker finds them by name.
    public Texture2D[] images;

    [Header("Controls")]
    // The key that opens and closes the guide.
    public Key openKey = Key.G;

    // Show the small "G  Step guide" hint in the corner?
    public bool showHint = true;

    // =================================================================
    // Two small classes that hold the guide once it has been read.
    // A Page has a title and a list of Blocks.
    // A Block is one piece of a page: a paragraph, picture, code box...
    // =================================================================
    private class Block
    {
        public string kind = "";                         // "p", "sub", "h3", "img", "code" or "box"
        public string text = "";                         // the main text (or the picture name / box colour)
        public string extra = "";                        // a caption or box title
        public List<string> lines = new List<string>();  // the lines of a code or box block
    }

    private class Page
    {
        public string title = "";
        public List<Block> blocks = new List<Block>();
    }

    // ---------- Colours used by the guide ----------
    private Color inkColour = new Color32(232, 236, 243, 255);
    private Color mutedColour = new Color32(160, 172, 190, 255);
    private Color amberColour = new Color32(242, 182, 92, 255);
    private Color skyColour = new Color32(159, 227, 255, 255);

    // ---------- Private variables ----------

    // All the pages, once the text has been read.
    private List<Page> pages = new List<Page>();

    // Which page is showing (0 = the first page).
    private int pageIndex = 0;

    // Is the guide open right now?
    private bool isOpen = false;

    // The scripts we switched off to freeze the player, so we can switch them back on.
    private List<MonoBehaviour> frozenScripts = new List<MonoBehaviour>();

    // Pieces of UI we need to find again later.
    private GameObject uiRoot;
    private GameObject panel;
    private GameObject hint;
    private RectTransform content;
    private ScrollRect scroll;
    private TextMeshProUGUI titleText;
    private TextMeshProUGUI counterText;

    // Used while reading the text file.
    private Page readingPage;
    private string readingParagraph = "";


    private void Start()
    {
        ReadGuide();
        BuildUI();
        SetOpen(false);
    }


    // Every frame, check the keyboard and mouse.
    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
        {
            return;
        }

        // G opens or closes the guide.
        if (keyboard[openKey].wasPressedThisFrame)
        {
            if (isOpen == true)
            {
                SetOpen(false);
            }
            else
            {
                SetOpen(true);
            }
            return;
        }

        // Everything below only matters while the guide is open.
        if (isOpen == false)
        {
            return;
        }

        // Esc closes the guide.
        if (keyboard.escapeKey.wasPressedThisFrame)
        {
            SetOpen(false);
            return;
        }

        // Right / D / Page Down: next page.
        if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame || keyboard.pageDownKey.wasPressedThisFrame)
        {
            ShowPage(pageIndex + 1);
        }

        // Left / A / Page Up: previous page.
        if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.pageUpKey.wasPressedThisFrame)
        {
            ShowPage(pageIndex - 1);
        }

        // Work out how much to scroll this frame.
        float scrollAmount = 0f;

        if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed)
        {
            scrollAmount = scrollAmount - 1.6f * Time.unscaledDeltaTime;
        }

        if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed)
        {
            scrollAmount = scrollAmount + 1.6f * Time.unscaledDeltaTime;
        }

        if (Mouse.current != null)
        {
            scrollAmount = scrollAmount + Mouse.current.scroll.ReadValue().y * 0.0012f;
        }

        if (scrollAmount != 0f)
        {
            ScrollBy(scrollAmount);
        }
    }


    // =================================================================
    // PUBLIC METHODS
    // =================================================================

    // Open (true) or close (false) the guide.
    public void SetOpen(bool open)
    {
        isOpen = open;

        if (panel != null)
        {
            panel.SetActive(open);
        }

        if (hint != null)
        {
            // Show the hint only when the guide is closed (and the hint is wanted).
            if (open == false && showHint == true)
            {
                hint.SetActive(true);
            }
            else
            {
                hint.SetActive(false);
            }
        }

        if (open == true)
        {
            ShowPage(pageIndex);
            FreezePlayer();
        }
        else
        {
            UnfreezePlayer();
        }
    }


    // Is the guide open?
    public bool IsGuideOpen()
    {
        return isOpen;
    }


    // How many pages the guide has.
    public int GetPageCount()
    {
        return pages.Count;
    }


    // Builds the UI and shows a page without pressing Play.
    // (Used to take screenshots of the guide.)
    public void PreviewPage(int index)
    {
        if (uiRoot == null)
        {
            ReadGuide();
            BuildUI();
        }

        isOpen = true;
        panel.SetActive(true);
        hint.SetActive(false);
        ShowPage(index);
    }


    // =================================================================
    // FREEZING THE PLAYER
    // We switch off the player's movement script (and any Press-E prompts)
    // while the guide is open, then switch them back on afterwards.
    // =================================================================

    private void FreezePlayer()
    {
        frozenScripts.Clear();

        MonoBehaviour[] allScripts = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);

        for (int i = 0; i < allScripts.Length; i++)
        {
            MonoBehaviour script = allScripts[i];

            if (script == null || script.enabled == false)
            {
                continue;
            }

            // GetType().Name is the name of the script, e.g. "FirstPersonController".
            string scriptName = script.GetType().Name;

            if (scriptName == "FirstPersonController" || scriptName == "ThirdPersonController" || scriptName == "InteractPrompt")
            {
                script.enabled = false;
                frozenScripts.Add(script);
            }
        }
    }


    private void UnfreezePlayer()
    {
        for (int i = 0; i < frozenScripts.Count; i++)
        {
            if (frozenScripts[i] != null)
            {
                frozenScripts[i].enabled = true;
            }
        }

        frozenScripts.Clear();
    }


    // =================================================================
    // READING THE GUIDE TEXT
    // Goes through the text file line by line and builds the list of pages.
    // =================================================================

    private void ReadGuide()
    {
        pages.Clear();
        readingPage = null;
        readingParagraph = "";

        if (guide == null)
        {
            return;
        }

        // Split the file into lines. ("\r" is removed so Windows line endings work.)
        string allText = guide.text.Replace("\r", "");
        string[] lines = allText.Split('\n');

        // While we are inside a =CODE or =BOX block, this holds it.
        Block openBlock = null;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];

            // Inside a code or box block: collect lines until =END.
            if (openBlock != null)
            {
                if (line.Trim() == "=END")
                {
                    readingPage.blocks.Add(openBlock);
                    openBlock = null;
                }
                else
                {
                    openBlock.lines.Add(line);
                }
                continue;
            }

            // A new page.
            if (line.StartsWith("=PAGE "))
            {
                FinishParagraph();
                readingPage = new Page();
                readingPage.title = line.Substring(6).Trim();
                pages.Add(readingPage);
                continue;
            }

            // Text before the first =PAGE goes onto an untitled page.
            if (readingPage == null)
            {
                readingPage = new Page();
                pages.Add(readingPage);
            }

            if (line.StartsWith("=SUB "))
            {
                FinishParagraph();
                AddSimpleBlock("sub", line.Substring(5));
            }
            else if (line.StartsWith("=H3 "))
            {
                FinishParagraph();
                AddSimpleBlock("h3", line.Substring(4));
            }
            else if (line.StartsWith("=IMG "))
            {
                FinishParagraph();

                // "name|caption": the part before | is the picture, after it is the caption.
                string[] parts = line.Substring(5).Split('|');
                Block picture = new Block();
                picture.kind = "img";
                picture.text = parts[0].Trim();
                if (parts.Length > 1)
                {
                    picture.extra = parts[1];
                }
                readingPage.blocks.Add(picture);
            }
            else if (line.StartsWith("=CODE"))
            {
                FinishParagraph();
                openBlock = new Block();
                openBlock.kind = "code";
                if (line.Length > 6)
                {
                    openBlock.text = line.Substring(6);
                }
            }
            else if (line.StartsWith("=BOX "))
            {
                FinishParagraph();

                // "#colour|Title"
                string[] parts = line.Substring(5).Split('|');
                openBlock = new Block();
                openBlock.kind = "box";
                openBlock.text = parts[0].Trim();
                if (parts.Length > 1)
                {
                    openBlock.extra = parts[1];
                }
            }
            else if (line.Trim().Length == 0)
            {
                // A blank line ends the paragraph.
                FinishParagraph();
            }
            else
            {
                // Normal text: add it to the paragraph we are building.
                if (readingParagraph.Length > 0)
                {
                    readingParagraph = readingParagraph + " ";
                }
                readingParagraph = readingParagraph + line;
            }
        }

        FinishParagraph();
    }


    // If we have been building a paragraph, add it to the page and start a new one.
    private void FinishParagraph()
    {
        if (readingParagraph.Length > 0 && readingPage != null)
        {
            AddSimpleBlock("p", readingParagraph.Trim());
        }

        readingParagraph = "";
    }


    // Add a block that is just one piece of text (a paragraph, subtitle or heading).
    private void AddSimpleBlock(string kind, string text)
    {
        Block block = new Block();
        block.kind = kind;
        block.text = text;
        readingPage.blocks.Add(block);
    }


    // =================================================================
    // BUILDING THE UI
    // Everything you see is made here in code: a Canvas, a dark card,
    // the title, a scrolling area, and the hint in the corner.
    // =================================================================

    private void BuildUI()
    {
        // ----- The Canvas (holds all the UI) -----
        uiRoot = new GameObject("Step Guide UI");
        uiRoot.transform.SetParent(transform, false);

        Canvas canvas = uiRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;   // draw on top of the scene's own UI

        CanvasScaler scaler = uiRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // ----- The "G  Step guide" hint, bottom right -----
        hint = MakeUIObject("Hint", uiRoot.transform);
        RectTransform hintRect = hint.GetComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(1f, 0f);
        hintRect.anchorMax = new Vector2(1f, 0f);
        hintRect.pivot = new Vector2(1f, 0f);
        hintRect.anchoredPosition = new Vector2(-28f, 28f);
        hintRect.sizeDelta = new Vector2(250f, 52f);
        hint.AddComponent<Image>().color = new Color(0.07f, 0.09f, 0.14f, 0.75f);

        TextMeshProUGUI hintText = MakeText(hint.transform, "<color=#F2B65C><b>G</b></color>   Step guide", 24f, inkColour, FontStyles.Normal);
        StretchInside(hintText.gameObject, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        hintText.alignment = TextAlignmentOptions.Center;

        // ----- A dark see-through layer over the whole screen -----
        panel = MakeUIObject("Guide Panel", uiRoot.transform);
        StretchInside(panel, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        // ----- The card in the middle -----
        GameObject card = MakeUIObject("Card", panel.transform);
        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(1300f, 980f);
        card.AddComponent<Image>().color = new Color32(27, 34, 50, 255);

        // An amber strip along the top of the card.
        GameObject accentBar = MakeUIObject("Accent", card.transform);
        StretchInside(accentBar, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -6f), Vector2.zero);
        accentBar.AddComponent<Image>().color = amberColour;

        // "LANTERN GROVE · STEP 01 · STEP GUIDE"
        TextMeshProUGUI kicker = MakeText(card.transform, "LANTERN GROVE  ·  " + stepLabel + "  ·  STEP GUIDE", 18f, amberColour, FontStyles.Bold);
        StretchInside(kicker.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -52f), new Vector2(-60f, -22f));

        // The page title.
        titleText = MakeText(card.transform, "", 40f, Color.white, FontStyles.Bold);
        StretchInside(titleText.gameObject, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -112f), new Vector2(-240f, -54f));
        titleText.textWrappingMode = TextWrappingModes.NoWrap;
        titleText.overflowMode = TextOverflowModes.Ellipsis;

        // "Page 3 / 11"
        counterText = MakeText(card.transform, "", 22f, mutedColour, FontStyles.Normal);
        StretchInside(counterText.gameObject, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-240f, -104f), new Vector2(-60f, -60f));
        counterText.alignment = TextAlignmentOptions.Right;

        // A thin line under the title.
        GameObject rule = MakeUIObject("Rule", card.transform);
        StretchInside(rule, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(60f, -124f), new Vector2(-60f, -122f));
        rule.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.12f);

        // ----- The scrolling area -----
        GameObject scrollObject = MakeUIObject("Scroll", card.transform);
        StretchInside(scrollObject, Vector2.zero, Vector2.one, new Vector2(60f, 76f), new Vector2(-60f, -136f));
        scroll = scrollObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;

        // The viewport is the "window" you see the content through.
        // RectMask2D hides anything outside it.
        GameObject viewport = MakeUIObject("Viewport", scrollObject.transform);
        StretchInside(viewport, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        viewport.AddComponent<RectMask2D>();
        scroll.viewport = viewport.GetComponent<RectTransform>();

        // The content holds the page's paragraphs, pictures and code.
        GameObject contentObject = MakeUIObject("Content", viewport.transform);
        content = contentObject.GetComponent<RectTransform>();
        content.anchorMin = new Vector2(0f, 1f);
        content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f);
        content.offsetMin = Vector2.zero;
        content.offsetMax = Vector2.zero;

        // A Vertical Layout Group stacks its children one under another.
        VerticalLayoutGroup layout = contentObject.AddComponent<VerticalLayoutGroup>();
        layout.spacing = 16f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 20, 6, 30);

        // A Content Size Fitter makes the content as tall as everything inside it.
        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scroll.content = content;

        // ----- The controls reminder at the bottom of the card -----
        string controls = "<b>Left / Right</b> or <b>A / D</b>: change page     <b>Up / Down</b> or <b>mouse wheel</b>: scroll     <b>G</b> or <b>Esc</b>: close";
        TextMeshProUGUI footer = MakeText(card.transform, controls, 19f, mutedColour, FontStyles.Normal);
        StretchInside(footer.gameObject, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(60f, 22f), new Vector2(-60f, 60f));
        footer.alignment = TextAlignmentOptions.Center;
    }


    // Make an empty UI object (a GameObject with a RectTransform) inside a parent.
    private GameObject MakeUIObject(string objectName, Transform parent)
    {
        GameObject uiObject = new GameObject(objectName, typeof(RectTransform));
        uiObject.transform.SetParent(parent, false);
        return uiObject;
    }


    // Make a piece of text inside a parent.
    private TextMeshProUGUI MakeText(Transform parent, string text, float size, Color colour, FontStyles style)
    {
        GameObject textObject = MakeUIObject("Text", parent);
        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.color = colour;
        label.fontStyle = style;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.raycastTarget = false;   // the text doesn't need to be clickable
        return label;
    }


    // Pin a UI object inside its parent using anchors and offsets.
    // Anchors 0 to 1 mean "stretch across the whole parent".
    // The offsets push the edges in (or out) by a number of pixels.
    private void StretchInside(GameObject uiObject, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        RectTransform rect = uiObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }


    // =================================================================
    // SHOWING A PAGE
    // =================================================================

    private void ShowPage(int index)
    {
        if (pages.Count == 0 || content == null)
        {
            return;
        }

        // Keep the page number between the first and last page.
        pageIndex = Mathf.Clamp(index, 0, pages.Count - 1);
        Page page = pages[pageIndex];

        titleText.text = page.title;
        counterText.text = "Page " + (pageIndex + 1) + " / " + pages.Count;

        // Remove everything from the previous page.
        ClearContent();

        // Add each block of this page.
        for (int i = 0; i < page.blocks.Count; i++)
        {
            Block block = page.blocks[i];

            if (block.kind == "p")
            {
                TextMeshProUGUI paragraph = MakeText(content, block.text, 25f, inkColour, FontStyles.Normal);
                paragraph.lineSpacing = 6f;
            }
            else if (block.kind == "sub")
            {
                MakeText(content, block.text, 27f, mutedColour, FontStyles.Italic);
            }
            else if (block.kind == "h3")
            {
                MakeText(content, block.text, 30f, skyColour, FontStyles.Bold);
            }
            else if (block.kind == "img")
            {
                AddPicture(block);
            }
            else if (block.kind == "code")
            {
                AddCode(block);
            }
            else if (block.kind == "box")
            {
                AddTipBox(block);
            }
        }

        // At the bottom, say what the next page is.
        if (pageIndex < pages.Count - 1)
        {
            string nextTitle = pages[pageIndex + 1].title;
            MakeText(content, "<color=#F2B65C>Next:</color> " + nextTitle + "  <color=#A0ACBE>(press Right)</color>", 22f, mutedColour, FontStyles.Italic);
        }

        // Make Unity lay out the new page straight away, then scroll to the top.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        scroll.verticalNormalizedPosition = 1f;
    }


    // Delete everything inside the content area.
    private void ClearContent()
    {
        // Go backwards, because removing children changes the numbering.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;

            // Hide it and take it out first, so the layout updates immediately.
            child.SetActive(false);
            child.transform.SetParent(null, false);

            // Destroy only works in Play mode; in Edit mode we need DestroyImmediate.
            if (Application.isPlaying)
            {
                Destroy(child);
            }
            else
            {
                DestroyImmediate(child);
            }
        }
    }


    // Scroll the page up (positive) or down (negative).
    private void ScrollBy(float amount)
    {
        if (scroll == null || content.rect.height <= 0f)
        {
            return;
        }

        // How much taller is the page than the window we see it through?
        float windowHeight = scroll.viewport.rect.height;
        float extraHeight = Mathf.Max(1f, content.rect.height - windowHeight);

        // verticalNormalizedPosition: 1 = top of the page, 0 = bottom.
        float newPosition = scroll.verticalNormalizedPosition + amount * 900f / extraHeight;
        scroll.verticalNormalizedPosition = Mathf.Clamp01(newPosition);
    }


    // Find a picture in the Images list by its name.
    private Texture2D FindImage(string imageName)
    {
        if (images == null)
        {
            return null;
        }

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null && images[i].name == imageName)
            {
                return images[i];
            }
        }

        return null;
    }


    // Add a picture (and its caption) to the page.
    private void AddPicture(Block block)
    {
        Texture2D picture = FindImage(block.text);
        if (picture == null)
        {
            return;
        }

        // 900 pixels wide, and tall enough to keep the picture's shape.
        float width = 900f;
        float height = width * picture.height / picture.width;

        // A holder that tells the layout how tall the picture is.
        GameObject holder = MakeUIObject("Image", content);
        LayoutElement layoutSize = holder.AddComponent<LayoutElement>();
        layoutSize.preferredHeight = height;
        layoutSize.minHeight = height;

        // The picture itself, centred in the holder.
        GameObject pictureObject = MakeUIObject("Picture", holder.transform);
        RectTransform pictureRect = pictureObject.GetComponent<RectTransform>();
        pictureRect.anchorMin = new Vector2(0.5f, 0.5f);
        pictureRect.anchorMax = new Vector2(0.5f, 0.5f);
        pictureRect.sizeDelta = new Vector2(width, height);

        RawImage rawImage = pictureObject.AddComponent<RawImage>();
        rawImage.texture = picture;
        rawImage.raycastTarget = false;

        // The caption underneath.
        if (block.extra.Length > 0)
        {
            TextMeshProUGUI caption = MakeText(content, block.extra, 20f, mutedColour, FontStyles.Italic);
            caption.alignment = TextAlignmentOptions.Center;
        }
    }


    // Add a code box with line numbers to the page.
    private void AddCode(Block block)
    {
        // The code's caption (e.g. the file name) above the box.
        if (block.text.Length > 0)
        {
            MakeText(content, block.text, 21f, skyColour, FontStyles.Bold);
        }

        // A dark box.
        GameObject box = MakeUIObject("Code", content);
        box.AddComponent<Image>().color = new Color32(14, 19, 31, 255);

        VerticalLayoutGroup boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(20, 20, 16, 16);
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;
        boxLayout.childForceExpandHeight = false;

        // Build the text one line at a time.
        //   <mspace> makes every letter the same width, like a code editor.
        //   <noparse> stops symbols such as < > in the code being read as formatting.
        string codeText = "<mspace=0.56em>";

        for (int i = 0; i < block.lines.Count; i++)
        {
            string lineNumber = (i + 1).ToString().PadLeft(3);
            codeText = codeText + "<color=#5E687A>" + lineNumber + "</color>  ";
            codeText = codeText + "<color=#DCE3EE><noparse>" + block.lines[i] + "</noparse></color>";

            // A new line after every line except the last.
            if (i < block.lines.Count - 1)
            {
                codeText = codeText + "\n";
            }
        }

        codeText = codeText + "</mspace>";

        TextMeshProUGUI codeLabel = MakeText(box.transform, codeText, 19f, inkColour, FontStyles.Normal);
        codeLabel.lineSpacing = 2f;
    }


    // Add a coloured tip box to the page.
    private void AddTipBox(Block block)
    {
        // The box's colour is written like "#7BC48A".
        // TryParseHtmlString turns that into a Color and puts it in "accent".
        // ("out" means the method fills the variable in for us.)
        Color accent = amberColour;
        ColorUtility.TryParseHtmlString(block.text, out accent);

        GameObject box = MakeUIObject("Box", content);
        box.AddComponent<Image>().color = new Color(accent.r, accent.g, accent.b, 0.14f);

        VerticalLayoutGroup boxLayout = box.AddComponent<VerticalLayoutGroup>();
        boxLayout.padding = new RectOffset(24, 24, 16, 18);
        boxLayout.spacing = 6f;
        boxLayout.childControlWidth = true;
        boxLayout.childControlHeight = true;
        boxLayout.childForceExpandHeight = false;

        // The box's title, e.g. "Tip".
        MakeText(box.transform, block.extra, 23f, accent, FontStyles.Bold);

        // The box's text: all of its lines joined into one paragraph.
        string boxText = "";
        for (int i = 0; i < block.lines.Count; i++)
        {
            if (i > 0)
            {
                boxText = boxText + " ";
            }
            boxText = boxText + block.lines[i];
        }

        MakeText(box.transform, boxText, 24f, inkColour, FontStyles.Normal);
    }
}
