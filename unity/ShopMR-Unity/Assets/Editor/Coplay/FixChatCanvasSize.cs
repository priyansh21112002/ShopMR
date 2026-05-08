using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Fixes ChatCanvas to match CatalogCanvas size:
/// - Scale: 0.001 (was 1.0 — making it 1000x too large!)
/// - Size: 800×600 (same as CatalogCanvas)
/// - Rebuilds ChatPanel layout to fit in 800×600
/// </summary>
public class FixChatCanvasSize
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        var chatCanvas = GameObject.Find("ChatCanvas");
        if (chatCanvas == null)
            return "ERROR: ChatCanvas not found";

        // ── Fix Canvas scale to match CatalogCanvas ──
        var canvasRT = chatCanvas.GetComponent<RectTransform>();
        canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
        canvasRT.sizeDelta = new Vector2(800f, 600f);
        log.AppendLine("✅ ChatCanvas scale set to (0.001, 0.001, 0.001), size 800×600");

        // ── Fix ChatPanel to fit 800×600 ──
        var chatPanel = chatCanvas.transform.Find("ChatPanel");
        if (chatPanel == null)
            return log + "\nERROR: ChatPanel not found";

        // ChatPanel stretches to fill canvas — just update the BoxCollider size
        var boxCol = chatPanel.GetComponent<BoxCollider>();
        if (boxCol != null)
        {
            boxCol.size = new Vector3(800f, 600f, 1f);
            log.AppendLine("✅ BoxCollider resized to 800×600");
        }

        // ── Fix ChatScrollView to fit within 800×600 layout ──
        // Title bar: 55px from top
        // Scroll area: from 55px top to 85px bottom 
        // Status text: 25px above input row
        // Input row: 60px from bottom
        var scrollView = chatPanel.Find("ChatScrollView");
        if (scrollView != null)
        {
            var svRT = scrollView.GetComponent<RectTransform>();
            svRT.anchorMin = new Vector2(0f, 0f);
            svRT.anchorMax = new Vector2(1f, 1f);
            svRT.offsetMin = new Vector2(10f, 90f);   // 90 from bottom (input row 60 + status 25 + gap 5)
            svRT.offsetMax = new Vector2(-10f, -55f);  // 55 from top (title bar)
            log.AppendLine("✅ ChatScrollView resized for 800×600");
        }

        // ── Fix StatusText position ──
        var statusText = chatPanel.Find("StatusText");
        if (statusText != null)
        {
            var statusRT = statusText.GetComponent<RectTransform>();
            statusRT.anchorMin = new Vector2(0f, 0f);
            statusRT.anchorMax = new Vector2(1f, 0f);
            statusRT.pivot = new Vector2(0.5f, 0f);
            statusRT.sizeDelta = new Vector2(0f, 22f);
            statusRT.anchoredPosition = new Vector2(0f, 65f);
            log.AppendLine("✅ StatusText repositioned");
        }

        // ── Fix InputRow ──
        var inputRow = chatPanel.Find("InputRow");
        if (inputRow != null)
        {
            var inputRowRT = inputRow.GetComponent<RectTransform>();
            inputRowRT.anchorMin = new Vector2(0f, 0f);
            inputRowRT.anchorMax = new Vector2(1f, 0f);
            inputRowRT.pivot = new Vector2(0.5f, 0f);
            inputRowRT.sizeDelta = new Vector2(-20f, 58f);
            inputRowRT.anchoredPosition = new Vector2(0f, 5f);
            log.AppendLine("✅ InputRow repositioned (58px height)");
        }

        // ── Fix TitleBar height ──
        var titleBar = chatPanel.Find("TitleBar");
        if (titleBar != null)
        {
            var titleBarRT = titleBar.GetComponent<RectTransform>();
            titleBarRT.anchorMin = new Vector2(0f, 1f);
            titleBarRT.anchorMax = new Vector2(1f, 1f);
            titleBarRT.pivot = new Vector2(0.5f, 1f);
            titleBarRT.sizeDelta = new Vector2(0f, 50f);
            titleBarRT.anchoredPosition = Vector2.zero;
            log.AppendLine("✅ TitleBar set to 50px height");

            // Fix title font size
            var titleText = titleBar.Find("TitleText");
            if (titleText != null)
            {
                var tmp = titleText.GetComponent<TMP_Text>();
                if (tmp != null) tmp.fontSize = 30f;
            }

            // Fix close button size
            var closeBtn = titleBar.Find("CloseButton");
            if (closeBtn != null)
            {
                var closeBtnRT = closeBtn.GetComponent<RectTransform>();
                closeBtnRT.sizeDelta = new Vector2(42f, 42f);
            }
        }

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
