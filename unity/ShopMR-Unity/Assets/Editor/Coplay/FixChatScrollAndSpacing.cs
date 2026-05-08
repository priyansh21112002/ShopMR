using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using TMPro;

public class FixChatScrollAndSpacing
{
    [MenuItem("ShopMR/Fix Chat Scroll and Spacing")]
    public static string Execute()
    {
        // Find the ChatScrollView
        var chatScrollViewGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView");
        if (chatScrollViewGO == null)
            return "ERROR: ChatScrollView not found";

        var scrollRect = chatScrollViewGO.GetComponent<ScrollRect>();
        if (scrollRect == null)
            return "ERROR: ScrollRect not found on ChatScrollView";

        // 1. Change movement type to Elastic (matches catalog)
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;

        // 2. Create Scrollbar Vertical (matching catalog scrollbar setup)
        // Check if scrollbar already exists
        var existingScrollbar = chatScrollViewGO.transform.Find("Scrollbar Vertical");
        if (existingScrollbar != null)
        {
            Object.DestroyImmediate(existingScrollbar.gameObject);
        }

        // Create Scrollbar Vertical
        var scrollbarGO = new GameObject("Scrollbar Vertical", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        scrollbarGO.transform.SetParent(chatScrollViewGO.transform, false);

        var scrollbarRect = scrollbarGO.GetComponent<RectTransform>();
        // Anchor to right side, full height
        scrollbarRect.anchorMin = new Vector2(1f, 0f);
        scrollbarRect.anchorMax = new Vector2(1f, 1f);
        scrollbarRect.anchoredPosition = Vector2.zero;
        scrollbarRect.sizeDelta = new Vector2(8f, 0f);
        scrollbarRect.pivot = new Vector2(1f, 0.5f);

        var scrollbarImage = scrollbarGO.GetComponent<Image>();
        scrollbarImage.color = new Color(0.1f, 0.1f, 0.12f, 1f); // dark track color

        // Create Sliding Area child
        var slidingAreaGO = new GameObject("Sliding Area", typeof(RectTransform));
        slidingAreaGO.transform.SetParent(scrollbarGO.transform, false);
        var slidingAreaRect = slidingAreaGO.GetComponent<RectTransform>();
        slidingAreaRect.anchorMin = Vector2.zero;
        slidingAreaRect.anchorMax = Vector2.one;
        slidingAreaRect.anchoredPosition = Vector2.zero;
        slidingAreaRect.sizeDelta = new Vector2(-10f, -10f);
        slidingAreaRect.pivot = new Vector2(0.5f, 0.5f);

        // Create Handle child
        var handleGO = new GameObject("Handle", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGO.transform.SetParent(slidingAreaGO.transform, false);
        var handleRect = handleGO.GetComponent<RectTransform>();
        handleRect.anchorMin = Vector2.zero;
        handleRect.anchorMax = new Vector2(1f, 0.2f);
        handleRect.anchoredPosition = Vector2.zero;
        handleRect.sizeDelta = new Vector2(10f, 10f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);

        var handleImage = handleGO.GetComponent<Image>();
        handleImage.color = new Color(0.4f, 0.4f, 0.45f, 1f); // lighter handle

        // Configure Scrollbar component
        var scrollbar = scrollbarGO.GetComponent<Scrollbar>();
        scrollbar.handleRect = handleRect;
        scrollbar.direction = Scrollbar.Direction.BottomToTop;
        scrollbar.targetGraphic = handleImage;

        // Configure color tint transition
        var colors = scrollbar.colors;
        colors.normalColor = new Color(0.4f, 0.4f, 0.45f, 1f);
        colors.highlightedColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        colors.pressedColor = new Color(0.3f, 0.3f, 0.35f, 1f);
        colors.selectedColor = new Color(0.55f, 0.55f, 0.6f, 1f);
        scrollbar.colors = colors;

        // 3. Assign scrollbar to ScrollRect
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3f;

        // 4. Fix Content spacing - increase from 8 to 16 for better readability
        var contentGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView/Viewport/Content");
        if (contentGO != null)
        {
            var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
            if (vlg != null)
            {
                vlg.spacing = 16f;
                // Add padding (top, bottom, left, right) for better visual separation
                vlg.padding = new RectOffset(10, 10, 10, 10);
            }
        }

        // 5. Mark scene dirty
        EditorUtility.SetDirty(chatScrollViewGO);
        if (contentGO != null) EditorUtility.SetDirty(contentGO);

        // Save scene
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveOpenScenes();

        return "SUCCESS: Chat scroll fixed - added scrollbar, elastic movement, increased spacing to 16 with padding";
    }
}
