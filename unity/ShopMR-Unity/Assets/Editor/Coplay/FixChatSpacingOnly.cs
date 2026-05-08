using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixChatSpacingOnly
{
    [MenuItem("ShopMR/Fix Chat Spacing Only")]
    public static string Execute()
    {
        // Fix Content spacing only - no size changes
        var contentGO = GameObject.Find("ChatCanvas/ChatPanel/ChatScrollView/Viewport/Content");
        if (contentGO == null)
            return "ERROR: Content not found";

        var vlg = contentGO.GetComponent<VerticalLayoutGroup>();
        if (vlg == null)
            return "ERROR: VerticalLayoutGroup not found";

        // Increase spacing from 8 to 16 for decent gap between messages
        vlg.spacing = 16f;
        // Add padding for better visual separation
        vlg.padding = new RectOffset(10, 10, 10, 10);

        EditorUtility.SetDirty(contentGO);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return $"SUCCESS: spacing={vlg.spacing}, padding=({vlg.padding.left},{vlg.padding.top},{vlg.padding.right},{vlg.padding.bottom})";
    }
}
