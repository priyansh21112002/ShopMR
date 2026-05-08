using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// Copies ALL size/scale/canvas settings from CatalogCanvas to ChatCanvas
/// so they are identical in world size.
/// </summary>
public class CopyCatalogSizeToChatCanvas
{
    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        var catalogCanvas = GameObject.Find("CatalogCanvas");
        var chatCanvas = GameObject.Find("ChatCanvas");
        if (catalogCanvas == null) return "ERROR: CatalogCanvas not found";
        if (chatCanvas == null) return "ERROR: ChatCanvas not found";

        // ── Copy RectTransform settings ──
        var catRT = catalogCanvas.GetComponent<RectTransform>();
        var chatRT = chatCanvas.GetComponent<RectTransform>();

        chatRT.localScale = catRT.localScale; // (0.001, 0.001, 0.001)
        chatRT.sizeDelta = catRT.sizeDelta;   // (800, 600)
        chatRT.pivot = catRT.pivot;
        chatRT.anchorMin = catRT.anchorMin;
        chatRT.anchorMax = catRT.anchorMax;
        log.AppendLine($"✅ RectTransform: scale={chatRT.localScale}, size={chatRT.sizeDelta}");

        // ── Copy Canvas settings ──
        var catCanvas = catalogCanvas.GetComponent<Canvas>();
        var chtCanvas = chatCanvas.GetComponent<Canvas>();

        chtCanvas.renderMode = catCanvas.renderMode;
        chtCanvas.worldCamera = catCanvas.worldCamera;
        chtCanvas.additionalShaderChannels = catCanvas.additionalShaderChannels;
        log.AppendLine($"✅ Canvas: renderMode={chtCanvas.renderMode}, camera={chtCanvas.worldCamera?.name}");

        // ── Copy CanvasScaler settings ──
        var catScaler = catalogCanvas.GetComponent<CanvasScaler>();
        var chtScaler = chatCanvas.GetComponent<CanvasScaler>();

        if (catScaler != null && chtScaler != null)
        {
            chtScaler.uiScaleMode = catScaler.uiScaleMode;
            chtScaler.scaleFactor = catScaler.scaleFactor;
            chtScaler.referencePixelsPerUnit = catScaler.referencePixelsPerUnit;
            chtScaler.referenceResolution = catScaler.referenceResolution;
            chtScaler.dynamicPixelsPerUnit = catScaler.dynamicPixelsPerUnit;
            log.AppendLine($"✅ CanvasScaler: dynamicPixelsPerUnit={chtScaler.dynamicPixelsPerUnit}, scaleFactor={chtScaler.scaleFactor}");
        }

        // ── Keep position hidden (will be placed by ChatPanelController.Show()) ──
        chatRT.localPosition = new Vector3(0f, -10f, 0f);
        chatRT.anchoredPosition = new Vector2(0f, -10f);

        // ── Mark dirty and save ──
        EditorUtility.SetDirty(chatCanvas);
        EditorUtility.SetDirty(chtCanvas);
        if (chtScaler != null) EditorUtility.SetDirty(chtScaler);

        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"✅ Scene saved: {scene.path}");

        return log.ToString();
    }
}
