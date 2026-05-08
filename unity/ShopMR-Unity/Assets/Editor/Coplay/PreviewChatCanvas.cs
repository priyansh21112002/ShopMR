using UnityEngine;
using UnityEditor;

public class PreviewChatCanvas
{
    public static string Execute()
    {
        var chatCanvas = GameObject.Find("ChatCanvas");
        if (chatCanvas == null) return "ERROR: ChatCanvas not found";

        // Temporarily move to visible position
        chatCanvas.transform.position = new Vector3(0f, 1.5f, 1.5f);

        var chatPanel = chatCanvas.transform.Find("ChatPanel");
        if (chatPanel == null) return "ERROR: ChatPanel not found";

        var cg = chatPanel.GetComponent<CanvasGroup>();
        if (cg != null)
        {
            cg.alpha = 1f;
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }

        return "ChatCanvas temporarily visible for preview";
    }
}
