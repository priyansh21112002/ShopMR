using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class HideChatCanvasBack
{
    public static string Execute()
    {
        var chatCanvas = GameObject.Find("ChatCanvas");
        if (chatCanvas == null) return "ERROR: ChatCanvas not found";

        // Move back off-screen
        chatCanvas.transform.position = new Vector3(0f, -10f, 0f);

        var chatPanel = chatCanvas.transform.Find("ChatPanel");
        if (chatPanel == null) return "ERROR: ChatPanel not found";

        var cg = chatPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 0f;
            cg.interactable = false;
            cg.blocksRaycasts = false;
        }

        // Disable colliders
        foreach (var col in chatPanel.GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        EditorUtility.SetDirty(chatCanvas);
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        return "✅ ChatCanvas hidden and scene saved";
    }
}
