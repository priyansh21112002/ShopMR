using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class ReconnectChatScrollbar
{
    [MenuItem("ShopMR/Reconnect Chat Scrollbar")]
    public static string Execute()
    {
        // Find the ChatScrollView
        var chatScrollViewGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView");
        if (chatScrollViewGO == null)
            return "ERROR: ChatScrollView not found";

        var scrollRect = chatScrollViewGO.GetComponent<ScrollRect>();
        if (scrollRect == null)
            return "ERROR: ScrollRect not found on ChatScrollView";

        // Find the existing Scrollbar Vertical
        var scrollbarTransform = chatScrollViewGO.transform.Find("Scrollbar Vertical");
        if (scrollbarTransform == null)
            return "ERROR: Scrollbar Vertical not found";

        var scrollbar = scrollbarTransform.GetComponent<Scrollbar>();
        if (scrollbar == null)
            return "ERROR: Scrollbar component not found on Scrollbar Vertical";

        // Re-apply scroll settings (DO NOT touch any sizes or positions)
        scrollRect.movementType = ScrollRect.MovementType.Elastic;
        scrollRect.elasticity = 0.1f;
        scrollRect.verticalScrollbar = scrollbar;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        scrollRect.verticalScrollbarSpacing = -3f;

        // Mark dirty and save
        EditorUtility.SetDirty(chatScrollViewGO);
        EditorUtility.SetDirty(scrollbarTransform.gameObject);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return "SUCCESS: Scrollbar reconnected, elastic movement set, visibility = AutoHideAndExpandViewport";
    }
}
