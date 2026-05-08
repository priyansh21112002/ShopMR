using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Fixes the ChatPanel to start hidden in the scene:
/// - Sets CanvasGroup alpha=0, interactable=false, blocksRaycasts=false
/// - Disables all colliders on the ChatPanel so it doesn't block placement raycasts
/// - Moves ChatCanvas far away initially (ChatPanelController repositions on Show)
/// </summary>
public class FixChatPanelInit
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        // Find ChatCanvas
        var chatCanvas = GameObject.Find("ChatCanvas");
        if (chatCanvas == null)
            return "ERROR: ChatCanvas not found";

        // Find ChatPanel
        var chatPanel = chatCanvas.transform.Find("ChatPanel");
        if (chatPanel == null)
            return "ERROR: ChatPanel not found under ChatCanvas";

        // Set CanvasGroup to hidden state
        var cg = chatPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = chatPanel.gameObject.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        cg.interactable = false;
        cg.blocksRaycasts = false;
        log.AppendLine("✅ CanvasGroup set to hidden (alpha=0, interactable=false, blocksRaycasts=false)");

        // Disable all colliders on ChatPanel and children
        var colliders = chatPanel.GetComponentsInChildren<Collider>(true);
        foreach (var col in colliders)
        {
            col.enabled = false;
            log.AppendLine($"   Disabled collider on: {col.gameObject.name}");
        }

        // Move ChatCanvas off-screen initially so it's not in the way
        // ChatPanelController.Show() will reposition it in front of the user
        chatCanvas.transform.position = new Vector3(0f, -10f, 0f);
        log.AppendLine("✅ Moved ChatCanvas to (0, -10, 0) — hidden below scene until Show() is called");

        EditorUtility.SetDirty(chatCanvas);
        EditorUtility.SetDirty(chatPanel.gameObject);

        // Save scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"✅ Scene saved: {scene.path}");

        return log.ToString();
    }
}
