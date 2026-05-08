using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;

/// <summary>
/// Fixes the ChatCanvas ISDK interactions by replacing the empty placeholder objects
/// with proper Meta SDK interaction templates (same approach as CatalogCanvas).
/// </summary>
public class FixChatCanvasInteractions
{
    // Template GUIDs from Meta Interaction SDK QuickActions (same as CatalogCanvas)
    private const string RAY_TEMPLATE_GUID  = "8369d93f7b6b99742bbea0649a41b7b1";
    private const string POKE_TEMPLATE_GUID = "4db41829582c7d24f80ee9603868dd67";

    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        // Find ChatCanvas
        GameObject chatCanvas = GameObject.Find("ChatCanvas");
        if (chatCanvas == null)
            return "ERROR: ChatCanvas not found in scene";

        Canvas canvas = chatCanvas.GetComponent<Canvas>();
        if (canvas == null)
            return "ERROR: ChatCanvas has no Canvas component";

        // Remove the empty placeholder ISDK objects
        var existingRay = chatCanvas.transform.Find("ISDK_RayCanvasInteraction");
        if (existingRay != null)
        {
            UnityEngine.Object.DestroyImmediate(existingRay.gameObject);
            log.AppendLine("Removed empty ISDK_RayCanvasInteraction");
        }

        var existingPoke = chatCanvas.transform.Find("ISDK_PokeCanvasInteraction");
        if (existingPoke != null)
        {
            UnityEngine.Object.DestroyImmediate(existingPoke.gameObject);
            log.AppendLine("Removed empty ISDK_PokeCanvasInteraction");
        }

        // Add proper Ray interaction from template
        bool rayAdded = AddInteractionTemplate(chatCanvas, canvas, RAY_TEMPLATE_GUID,
            "ISDK_RayCanvasInteraction", log);

        // Add proper Poke interaction from template
        bool pokeAdded = AddInteractionTemplate(chatCanvas, canvas, POKE_TEMPLATE_GUID,
            "ISDK_PokeCanvasInteraction", log);

        // Save
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        log.AppendLine($"Saved scene: {scene.path}");

        return log.ToString();
    }

    private static bool AddInteractionTemplate(GameObject canvasGO, Canvas canvas,
        string templateGuid, string displayName, System.Text.StringBuilder log)
    {
        string prefabPath = AssetDatabase.GUIDToAssetPath(templateGuid);
        if (string.IsNullOrEmpty(prefabPath))
        {
            log.AppendLine($"⚠️  Could not find template prefab for GUID: {templateGuid}");
            return false;
        }

        GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (templatePrefab == null)
        {
            log.AppendLine($"⚠️  Could not load template prefab: {prefabPath}");
            return false;
        }

        // Instantiate as a regular copy (not prefab link) - matches wizard behavior
        GameObject instance = UnityEngine.Object.Instantiate(templatePrefab);
        instance.name = displayName;
        instance.transform.SetParent(canvasGO.transform, false);

        // Reset RectTransform to stretch-fit
        RectTransform rt = instance.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.localPosition = Vector3.zero;
            rt.localRotation = Quaternion.identity;
            rt.localScale = Vector3.one;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
        }

        // Inject canvas reference into PointableCanvas
        var pointableCanvas = instance.GetComponent<PointableCanvas>();
        if (pointableCanvas != null)
        {
            pointableCanvas.InjectCanvas(canvas);
            log.AppendLine($"✅ Added {displayName} and wired PointableCanvas → Canvas");
        }
        else
        {
            log.AppendLine($"⚠️  {displayName} has no PointableCanvas component");
        }

        return true;
    }
}
