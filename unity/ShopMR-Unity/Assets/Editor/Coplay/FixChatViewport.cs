using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixChatViewport
{
    [MenuItem("ShopMR/Fix Chat Viewport")]
    public static string Execute()
    {
        // Fix the Viewport - its anchors got corrupted by AutoHideAndExpandViewport
        var viewportGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView/Viewport");
        if (viewportGO == null)
            return "ERROR: Viewport not found";

        var viewportRect = viewportGO.GetComponent<RectTransform>();

        // Restore viewport to stretch full parent (matching how catalog viewport works)
        viewportRect.anchorMin = new Vector2(0f, 0f);
        viewportRect.anchorMax = new Vector2(1f, 1f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;
        viewportRect.pivot = new Vector2(0f, 1f);

        // Fix ScrollRect - use Permanent visibility instead of AutoHideAndExpandViewport
        // AutoHideAndExpandViewport corrupts viewport anchors on save
        var scrollViewGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView");
        if (scrollViewGO != null)
        {
            var scrollRect = scrollViewGO.GetComponent<ScrollRect>();
            if (scrollRect != null)
            {
                // Keep elastic and scrollbar, but use Permanent visibility
                // which does NOT modify the viewport rect
                scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
                scrollRect.verticalScrollbarSpacing = 0f;
                EditorUtility.SetDirty(scrollViewGO);
            }
        }

        EditorUtility.SetDirty(viewportGO);

        // Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return $"SUCCESS: Viewport anchors restored to stretch (0,0)-(1,1), scrollbar visibility set to Permanent";
    }
}
