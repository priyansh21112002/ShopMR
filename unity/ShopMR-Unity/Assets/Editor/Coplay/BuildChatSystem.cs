using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.Text;

/// <summary>
/// Builds the entire Chat system:
/// 1. ChatMessageBubble.prefab
/// 2. ChatPanel.prefab
/// 3. ChatCanvas in the scene with ISDK interactions
/// </summary>
public class BuildChatSystem
{
    public static string Execute()
    {
        var log = new StringBuilder();

        // ── Step 1: Build ChatMessageBubble prefab ──
        string bubblePrefabPath = BuildChatMessageBubblePrefab(log);

        // ── Step 2: Build ChatPanel prefab ──
        string panelPrefabPath = BuildChatPanelPrefab(log, bubblePrefabPath);

        // ── Step 3: Add ChatCanvas to scene ──
        AddChatCanvasToScene(log, panelPrefabPath);

        // ── Step 4: Save scene ──
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"Saved scene: {scene.path}");

        return log.ToString();
    }

    private static string BuildChatMessageBubblePrefab(StringBuilder log)
    {
        string prefabPath = "Assets/_ShopMR/Prefabs/ChatMessageBubble.prefab";

        // Root: ChatMessageBubble
        GameObject root = new GameObject("ChatMessageBubble");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(680f, 60f); // default height, auto-resized by ContentSizeFitter

        // Add LayoutElement for flexible width
        var layoutElem = root.AddComponent<LayoutElement>();
        layoutElem.flexibleWidth = 1f;
        layoutElem.minHeight = 40f;

        // ContentSizeFitter to auto-size height
        var csf = root.AddComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Add ChatMessageUI component
        var chatMsgUI = root.AddComponent<ShopMR.Chat.ChatMessageUI>();

        // Background child
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(root.transform, false);
        var bgRT = bgGO.AddComponent<RectTransform>();
        bgRT.anchorMin = Vector2.zero;
        bgRT.anchorMax = Vector2.one;
        bgRT.offsetMin = Vector2.zero;
        bgRT.offsetMax = Vector2.zero;
        var bgImage = bgGO.AddComponent<Image>();
        bgImage.color = new Color(0.22f, 0.22f, 0.25f, 1f);
        bgImage.raycastTarget = false;

        // MessageText child
        GameObject textGO = new GameObject("MessageText");
        textGO.transform.SetParent(root.transform, false);
        var textRT = textGO.AddComponent<RectTransform>();
        textRT.anchorMin = Vector2.zero;
        textRT.anchorMax = Vector2.one;
        textRT.offsetMin = new Vector2(12f, 8f);
        textRT.offsetMax = new Vector2(-12f, -8f);
        var tmpText = textGO.AddComponent<TextMeshProUGUI>();
        tmpText.text = "Message text";
        tmpText.fontSize = 24f;
        tmpText.color = Color.white;
        tmpText.enableWordWrapping = true;
        tmpText.alignment = TextAlignmentOptions.TopLeft;
        tmpText.raycastTarget = false;

        // Wire references
        chatMsgUI.messageText = tmpText;
        chatMsgUI.bubbleBackground = bgImage;

        // Save prefab
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        log.AppendLine($"✅ Created ChatMessageBubble prefab at {prefabPath}");
        return prefabPath;
    }

    private static string BuildChatPanelPrefab(StringBuilder log, string bubblePrefabPath)
    {
        string prefabPath = "Assets/_ShopMR/Prefabs/ChatPanel.prefab";

        // Load the bubble prefab for reference assignment
        GameObject bubblePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(bubblePrefabPath);

        // ── Root: ChatPanel (800x700) ──
        GameObject root = new GameObject("ChatPanel");
        var rootRT = root.AddComponent<RectTransform>();
        rootRT.sizeDelta = new Vector2(800f, 700f);

        // CanvasGroup for hide/show
        root.AddComponent<CanvasGroup>();

        // Background Image (dark panel like catalog)
        var rootImage = root.AddComponent<Image>();
        rootImage.color = new Color(0.078f, 0.078f, 0.098f, 0.96f); // same as CatalogPanel

        // BoxCollider for ISDK interactions
        var boxCol = root.AddComponent<BoxCollider>();
        boxCol.size = new Vector3(800f, 700f, 1f);
        boxCol.center = Vector3.zero;

        // ── TitleBar (height=60, top) ──
        GameObject titleBar = CreateChild("TitleBar", root.transform);
        var titleBarRT = titleBar.GetComponent<RectTransform>();
        titleBarRT.anchorMin = new Vector2(0f, 1f);
        titleBarRT.anchorMax = new Vector2(1f, 1f);
        titleBarRT.pivot = new Vector2(0.5f, 1f);
        titleBarRT.sizeDelta = new Vector2(0f, 60f);
        titleBarRT.anchoredPosition = Vector2.zero;

        // TitleText
        GameObject titleTextGO = CreateChild("TitleText", titleBar.transform);
        var titleTextRT = titleTextGO.GetComponent<RectTransform>();
        titleTextRT.anchorMin = Vector2.zero;
        titleTextRT.anchorMax = Vector2.one;
        titleTextRT.offsetMin = new Vector2(20f, 0f);
        titleTextRT.offsetMax = new Vector2(-60f, 0f);
        var titleTMP = titleTextGO.AddComponent<TextMeshProUGUI>();
        titleTMP.text = "ShopMR Assistant";
        titleTMP.fontSize = 36f;
        titleTMP.color = Color.white;
        titleTMP.alignment = TextAlignmentOptions.MidlineLeft;
        titleTMP.raycastTarget = false;

        // CloseButton (top-right X, 50x50)
        GameObject closeBtnGO = CreateChild("CloseButton", titleBar.transform);
        var closeBtnRT = closeBtnGO.GetComponent<RectTransform>();
        closeBtnRT.anchorMin = new Vector2(1f, 0.5f);
        closeBtnRT.anchorMax = new Vector2(1f, 0.5f);
        closeBtnRT.pivot = new Vector2(1f, 0.5f);
        closeBtnRT.sizeDelta = new Vector2(50f, 50f);
        closeBtnRT.anchoredPosition = new Vector2(-5f, 0f);
        var closeBtnImage = closeBtnGO.AddComponent<Image>();
        closeBtnImage.color = new Color(0.6f, 0.2f, 0.2f, 1f);
        var closeBtn = closeBtnGO.AddComponent<Button>();
        closeBtn.targetGraphic = closeBtnImage;
        // X label
        GameObject xLabel = CreateChild("Label", closeBtnGO.transform);
        var xLabelRT = xLabel.GetComponent<RectTransform>();
        xLabelRT.anchorMin = Vector2.zero;
        xLabelRT.anchorMax = Vector2.one;
        xLabelRT.offsetMin = Vector2.zero;
        xLabelRT.offsetMax = Vector2.zero;
        var xTMP = xLabel.AddComponent<TextMeshProUGUI>();
        xTMP.text = "X";
        xTMP.fontSize = 28f;
        xTMP.color = Color.white;
        xTMP.alignment = TextAlignmentOptions.Center;
        xTMP.raycastTarget = false;

        // ── ChatScrollView (anchored below title bar, above input row) ──
        GameObject scrollViewGO = CreateChild("ChatScrollView", root.transform);
        var svRT = scrollViewGO.GetComponent<RectTransform>();
        svRT.anchorMin = new Vector2(0f, 0f);
        svRT.anchorMax = new Vector2(1f, 1f);
        svRT.offsetMin = new Vector2(10f, 100f);   // 100 from bottom (input row + status)
        svRT.offsetMax = new Vector2(-10f, -65f);  // 65 from top (title bar)
        var svImage = scrollViewGO.AddComponent<Image>();
        svImage.color = new Color(0.05f, 0.05f, 0.07f, 0.8f);
        svImage.raycastTarget = true;
        var scrollRect = scrollViewGO.AddComponent<ScrollRect>();
        scrollRect.horizontal = false;
        scrollRect.vertical = true;
        scrollRect.movementType = ScrollRect.MovementType.Clamped;

        // Viewport
        GameObject viewport = CreateChild("Viewport", scrollViewGO.transform);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = Vector2.zero;
        vpRT.offsetMax = Vector2.zero;
        viewport.AddComponent<RectMask2D>();

        // Content (VerticalLayoutGroup + ContentSizeFitter)
        GameObject content = CreateChild("Content", viewport.transform);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0f, 1f);
        contentRT.anchorMax = new Vector2(1f, 1f);
        contentRT.pivot = new Vector2(0.5f, 1f);
        contentRT.sizeDelta = new Vector2(0f, 0f);
        contentRT.anchoredPosition = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childControlHeight = true;
        vlg.childControlWidth = true;
        var contentCSF = content.AddComponent<ContentSizeFitter>();
        contentCSF.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Wire ScrollRect
        scrollRect.viewport = vpRT;
        scrollRect.content = contentRT;

        // ── StatusText (below scroll, above input row) ──
        GameObject statusTextGO = CreateChild("StatusText", root.transform);
        var statusRT = statusTextGO.GetComponent<RectTransform>();
        statusRT.anchorMin = new Vector2(0f, 0f);
        statusRT.anchorMax = new Vector2(1f, 0f);
        statusRT.pivot = new Vector2(0.5f, 0f);
        statusRT.sizeDelta = new Vector2(0f, 25f);
        statusRT.anchoredPosition = new Vector2(0f, 75f);
        var statusTMP = statusTextGO.AddComponent<TextMeshProUGUI>();
        statusTMP.text = "";
        statusTMP.fontSize = 20f;
        statusTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
        statusTMP.alignment = TextAlignmentOptions.Center;
        statusTMP.raycastTarget = false;

        // ── InputRow (bottom, 70px height) ──
        GameObject inputRow = CreateChild("InputRow", root.transform);
        var inputRowRT = inputRow.GetComponent<RectTransform>();
        inputRowRT.anchorMin = new Vector2(0f, 0f);
        inputRowRT.anchorMax = new Vector2(1f, 0f);
        inputRowRT.pivot = new Vector2(0.5f, 0f);
        inputRowRT.sizeDelta = new Vector2(-20f, 70f);  // 10px padding each side
        inputRowRT.anchoredPosition = new Vector2(0f, 5f);
        var hlg = inputRow.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 8f;
        hlg.padding = new RectOffset(0, 0, 5, 5);
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = true;
        hlg.childControlHeight = true;
        hlg.childControlWidth = true;

        // InputField (TMP)
        GameObject inputFieldGO = CreateChild("InputField", inputRow.transform);
        var ifLE = inputFieldGO.AddComponent<LayoutElement>();
        ifLE.flexibleWidth = 1f;
        ifLE.minHeight = 60f;
        var ifImage = inputFieldGO.AddComponent<Image>();
        ifImage.color = new Color(0.15f, 0.15f, 0.18f, 1f);
        var inputFieldComp = inputFieldGO.AddComponent<TMP_InputField>();

        // Text Area inside InputField
        GameObject textArea = CreateChild("Text Area", inputFieldGO.transform);
        var textAreaRT = textArea.GetComponent<RectTransform>();
        textAreaRT.anchorMin = Vector2.zero;
        textAreaRT.anchorMax = Vector2.one;
        textAreaRT.offsetMin = new Vector2(10f, 5f);
        textAreaRT.offsetMax = new Vector2(-10f, -5f);
        textArea.AddComponent<RectMask2D>();

        // Placeholder
        GameObject placeholder = CreateChild("Placeholder", textArea.transform);
        var phRT = placeholder.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero;
        phRT.anchorMax = Vector2.one;
        phRT.offsetMin = Vector2.zero;
        phRT.offsetMax = Vector2.zero;
        var phTMP = placeholder.AddComponent<TextMeshProUGUI>();
        phTMP.text = "Ask about furniture...";
        phTMP.fontSize = 24f;
        phTMP.color = new Color(0.5f, 0.5f, 0.5f, 0.7f);
        phTMP.fontStyle = FontStyles.Italic;
        phTMP.raycastTarget = false;

        // Input Text
        GameObject inputText = CreateChild("Text", textArea.transform);
        var itRT = inputText.GetComponent<RectTransform>();
        itRT.anchorMin = Vector2.zero;
        itRT.anchorMax = Vector2.one;
        itRT.offsetMin = Vector2.zero;
        itRT.offsetMax = Vector2.zero;
        var itTMP = inputText.AddComponent<TextMeshProUGUI>();
        itTMP.text = "";
        itTMP.fontSize = 24f;
        itTMP.color = Color.white;
        itTMP.raycastTarget = false;

        // Wire TMP_InputField
        inputFieldComp.textViewport = textAreaRT;
        inputFieldComp.textComponent = itTMP;
        inputFieldComp.placeholder = phTMP;
        inputFieldComp.fontAsset = itTMP.font;
        inputFieldComp.pointSize = 24f;

        // Send Button
        GameObject sendBtnGO = CreateChild("SendButton", inputRow.transform);
        var sendLE = sendBtnGO.AddComponent<LayoutElement>();
        sendLE.minWidth = 120f;
        sendLE.preferredWidth = 120f;
        sendLE.minHeight = 60f;
        var sendImage = sendBtnGO.AddComponent<Image>();
        sendImage.color = new Color(0.2f, 0.45f, 0.8f, 1f);
        var sendBtn = sendBtnGO.AddComponent<Button>();
        sendBtn.targetGraphic = sendImage;
        // Send label
        GameObject sendLabel = CreateChild("Label", sendBtnGO.transform);
        var sendLabelRT = sendLabel.GetComponent<RectTransform>();
        sendLabelRT.anchorMin = Vector2.zero;
        sendLabelRT.anchorMax = Vector2.one;
        sendLabelRT.offsetMin = Vector2.zero;
        sendLabelRT.offsetMax = Vector2.zero;
        var sendTMP = sendLabel.AddComponent<TextMeshProUGUI>();
        sendTMP.text = "Send";
        sendTMP.fontSize = 26f;
        sendTMP.color = Color.white;
        sendTMP.alignment = TextAlignmentOptions.Center;
        sendTMP.raycastTarget = false;

        // ── Add ChatPanelController component ──
        var chatController = root.AddComponent<ShopMR.Chat.ChatPanelController>();

        // Wire serialized fields via SerializedObject
        // First save as prefab, then use SerializedObject
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
        Object.DestroyImmediate(root);

        // Now wire references using SerializedObject on the saved prefab
        var chatCtrl = prefab.GetComponent<ShopMR.Chat.ChatPanelController>();
        var so = new SerializedObject(chatCtrl);

        // Find references within the prefab
        var prefabContent = prefab.transform.Find("ChatScrollView/Viewport/Content");
        var prefabInputField = prefab.transform.Find("InputRow/InputField");
        var prefabSendBtn = prefab.transform.Find("InputRow/SendButton");
        var prefabCloseBtn = prefab.transform.Find("TitleBar/CloseButton");
        var prefabStatusText = prefab.transform.Find("StatusText");
        var prefabScrollView = prefab.transform.Find("ChatScrollView");

        so.FindProperty("chatMessageBubblePrefab").objectReferenceValue = bubblePrefab;
        so.FindProperty("chatContent").objectReferenceValue = prefabContent;
        so.FindProperty("inputField").objectReferenceValue = prefabInputField.GetComponent<TMP_InputField>();
        so.FindProperty("sendButton").objectReferenceValue = prefabSendBtn.GetComponent<Button>();
        so.FindProperty("closeButton").objectReferenceValue = prefabCloseBtn.GetComponent<Button>();
        so.FindProperty("statusText").objectReferenceValue = prefabStatusText.GetComponent<TMP_Text>();
        so.FindProperty("scrollRect").objectReferenceValue = prefabScrollView.GetComponent<ScrollRect>();
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(prefab);
        AssetDatabase.SaveAssets();

        log.AppendLine($"✅ Created ChatPanel prefab at {prefabPath}");
        return prefabPath;
    }

    private static void AddChatCanvasToScene(StringBuilder log, string panelPrefabPath)
    {
        // Check if ChatCanvas already exists
        GameObject existingCanvas = GameObject.Find("ChatCanvas");
        if (existingCanvas != null)
        {
            Object.DestroyImmediate(existingCanvas);
            log.AppendLine("Removed existing ChatCanvas");
        }

        // Load ChatPanel prefab
        GameObject panelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(panelPrefabPath);

        // ── Create ChatCanvas (World Space) ──
        GameObject canvasGO = new GameObject("ChatCanvas");
        Undo.RegisterCreatedObjectUndo(canvasGO, "Create ChatCanvas");

        var canvasRT = canvasGO.AddComponent<RectTransform>();
        canvasRT.localPosition = new Vector3(0f, 1.5f, 1.5f);
        canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.sizeDelta = new Vector2(800f, 700f);

        var canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.additionalShaderChannels = AdditionalCanvasShaderChannels.TexCoord1
            | AdditionalCanvasShaderChannels.Normal
            | AdditionalCanvasShaderChannels.Tangent;

        // Find CenterEyeAnchor for event camera
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            var cam = centerEye.GetComponent<Camera>();
            if (cam != null)
                canvas.worldCamera = cam;
        }

        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Add OVRRaycaster (same as CatalogCanvas)
        var ovrRaycaster = canvasGO.AddComponent<OVRRaycaster>();
        log.AppendLine("✅ Created ChatCanvas (World Space)");

        // ── Instantiate ChatPanel as child ──
        GameObject panelInstance = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab, canvasGO.transform);
        var panelRT = panelInstance.GetComponent<RectTransform>();
        panelRT.anchorMin = Vector2.zero;
        panelRT.anchorMax = Vector2.one;
        panelRT.offsetMin = Vector2.zero;
        panelRT.offsetMax = Vector2.zero;
        panelRT.localScale = Vector3.one;
        panelRT.localPosition = Vector3.zero;
        log.AppendLine("✅ Added ChatPanel instance to ChatCanvas (stretch-stretch)");

        // ── Add ISDK Ray + Poke interaction (same structure as CatalogCanvas) ──
        AddISDKInteractions(canvasGO, log);

        EditorUtility.SetDirty(canvasGO);
    }

    private static void AddISDKInteractions(GameObject canvasGO, StringBuilder log)
    {
        // Try to add the same interaction components as CatalogCanvas has
        // These are: RayInteractable, PointableCanvas, PokeInteractable
        // We use reflection-safe approach since Meta SDK types may not always resolve at edit time

        try
        {
            // ── ISDK_RayCanvasInteraction ──
            GameObject rayInteraction = CreateChild("ISDK_RayCanvasInteraction", canvasGO.transform);
            var rayRT = rayInteraction.GetComponent<RectTransform>();
            rayRT.anchorMin = Vector2.zero;
            rayRT.anchorMax = Vector2.one;
            rayRT.offsetMin = Vector2.zero;
            rayRT.offsetMax = Vector2.zero;

            // Add LayoutElement (ignoreLayout to not affect parent layout)
            var rayLE = rayInteraction.AddComponent<LayoutElement>();
            rayLE.ignoreLayout = true;

            // Try to add Meta SDK interaction components
            var rayInteractableType = System.Type.GetType("Oculus.Interaction.RayInteractable, Oculus.Interaction.Runtime");
            var pointableCanvasType = System.Type.GetType("Oculus.Interaction.PointableCanvas, Oculus.Interaction.Runtime");

            if (rayInteractableType != null)
                rayInteraction.AddComponent(rayInteractableType);
            if (pointableCanvasType != null)
            {
                var pc = rayInteraction.AddComponent(pointableCanvasType);
                // Wire the Canvas reference
                var canvasField = pointableCanvasType.GetField("_canvas",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (canvasField != null)
                    canvasField.SetValue(pc, canvasGO.GetComponent<Canvas>());
            }

            // Surface child
            GameObject raySurface = CreateChild("Surface", rayInteraction.transform);
            var raySurfRT = raySurface.GetComponent<RectTransform>();
            raySurfRT.anchorMin = Vector2.zero;
            raySurfRT.anchorMax = Vector2.one;
            raySurfRT.offsetMin = Vector2.zero;
            raySurfRT.offsetMax = Vector2.zero;

            var planeSurfaceType = System.Type.GetType("Oculus.Interaction.Surfaces.PlaneSurface, Oculus.Interaction.Runtime");
            var clippedPlaneType = System.Type.GetType("Oculus.Interaction.Surfaces.ClippedPlaneSurface, Oculus.Interaction.Runtime");
            var boundsClipperType = System.Type.GetType("Oculus.Interaction.Surfaces.BoundsClipper, Oculus.Interaction.Runtime");

            if (planeSurfaceType != null) raySurface.AddComponent(planeSurfaceType);
            if (clippedPlaneType != null) raySurface.AddComponent(clippedPlaneType);
            if (boundsClipperType != null) raySurface.AddComponent(boundsClipperType);

            log.AppendLine("✅ Added ISDK_RayCanvasInteraction");

            // ── ISDK_PokeCanvasInteraction ──
            GameObject pokeInteraction = CreateChild("ISDK_PokeCanvasInteraction", canvasGO.transform);
            var pokeRT = pokeInteraction.GetComponent<RectTransform>();
            pokeRT.anchorMin = Vector2.zero;
            pokeRT.anchorMax = Vector2.one;
            pokeRT.offsetMin = Vector2.zero;
            pokeRT.offsetMax = Vector2.zero;

            var pokeLE = pokeInteraction.AddComponent<LayoutElement>();
            pokeLE.ignoreLayout = true;

            var pokeInteractableType = System.Type.GetType("Oculus.Interaction.PokeInteractable, Oculus.Interaction.Runtime");
            if (pokeInteractableType != null)
                pokeInteraction.AddComponent(pokeInteractableType);
            if (pointableCanvasType != null)
            {
                var pc2 = pokeInteraction.AddComponent(pointableCanvasType);
                var canvasField2 = pointableCanvasType.GetField("_canvas",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (canvasField2 != null)
                    canvasField2.SetValue(pc2, canvasGO.GetComponent<Canvas>());
            }

            // Surface child for poke
            GameObject pokeSurface = CreateChild("Surface", pokeInteraction.transform);
            var pokeSurfRT = pokeSurface.GetComponent<RectTransform>();
            pokeSurfRT.anchorMin = Vector2.zero;
            pokeSurfRT.anchorMax = Vector2.one;
            pokeSurfRT.offsetMin = Vector2.zero;
            pokeSurfRT.offsetMax = Vector2.zero;

            if (planeSurfaceType != null) pokeSurface.AddComponent(planeSurfaceType);
            if (clippedPlaneType != null) pokeSurface.AddComponent(clippedPlaneType);
            if (boundsClipperType != null) pokeSurface.AddComponent(boundsClipperType);

            log.AppendLine("✅ Added ISDK_PokeCanvasInteraction");
        }
        catch (System.Exception ex)
        {
            log.AppendLine($"⚠️ Could not add ISDK interactions (Meta SDK types may not be accessible): {ex.Message}");
            log.AppendLine("   The ChatCanvas will still work with standard GraphicRaycaster + OVRRaycaster.");
        }
    }

    private static GameObject CreateChild(string name, Transform parent)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        return go;
    }
}
