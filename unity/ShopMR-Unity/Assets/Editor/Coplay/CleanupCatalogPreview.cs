using UnityEngine;
using UnityEditor;
using TMPro;

public class CleanupCatalogPreview
{
    public static string Execute()
    {
        int removed = 0;

        // Remove mock category buttons
        var catRow = GameObject.Find("CatalogCanvas/CatalogPanel/CategoryRow");
        if (catRow != null)
        {
            for (int i = catRow.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(catRow.transform.GetChild(i).gameObject);
                removed++;
            }
        }

        // Remove mock product cards
        var content = GameObject.Find("CatalogCanvas/CatalogPanel/CardScrollView/Viewport/Content");
        if (content != null)
        {
            for (int i = content.transform.childCount - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(content.transform.GetChild(i).gameObject);
                removed++;
            }
        }

        // Reset status text
        var statusGO = GameObject.Find("CatalogCanvas/CatalogPanel/StatusText");
        if (statusGO != null)
        {
            var tmp = statusGO.GetComponent<TMP_Text>();
            if (tmp != null) tmp.text = "Loading catalog\u2026";
        }

        // Reset CanvasGroup to hidden (startHidden = true in CatalogPanelController)
        var panel = GameObject.Find("CatalogCanvas");
        if (panel != null)
        {
            var cg = panel.GetComponent<CanvasGroup>();
            if (cg == null) cg = panel.GetComponentInChildren<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0f;
                cg.interactable = false;
                cg.blocksRaycasts = false;
            }
        }

        // Mark scene dirty so changes are saved
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

        return $"Removed {removed} mock objects. Panel reset to hidden state for runtime.";
    }
}
