using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.EventSystems;

public class FixCanvasInteraction
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // ── 1. Add OVRRaycaster to CatalogCanvas ──
        var catalogCanvas = GameObject.Find("CatalogCanvas");
        if (catalogCanvas != null)
        {
            // Add OVRRaycaster if not already present
            var ovrRaycaster = catalogCanvas.GetComponent<OVRRaycaster>();
            if (ovrRaycaster == null)
            {
                ovrRaycaster = catalogCanvas.AddComponent<OVRRaycaster>();
                results.AppendLine("Added OVRRaycaster to CatalogCanvas");
            }
            else
            {
                results.AppendLine("OVRRaycaster already present on CatalogCanvas");
            }

            // Keep GraphicRaycaster for Editor mouse support; OVRRaycaster handles Quest
            var graphicRaycaster = catalogCanvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (graphicRaycaster != null)
            {
                results.AppendLine("GraphicRaycaster kept for Editor compatibility");
            }

            EditorUtility.SetDirty(catalogCanvas);
        }
        else
        {
            results.AppendLine("WARNING: CatalogCanvas not found");
        }

        // ── 2. Add OVRInputModule to EventSystem ──
        var eventSystem = GameObject.Find("EventSystem");
        if (eventSystem != null)
        {
            var ovrInput = eventSystem.GetComponent<OVRInputModule>();
            if (ovrInput == null)
            {
                ovrInput = eventSystem.AddComponent<OVRInputModule>();
                results.AppendLine("Added OVRInputModule to EventSystem");
            }
            else
            {
                results.AppendLine("OVRInputModule already present on EventSystem");
            }

            // Configure OVRInputModule
            // rayTransform = right controller anchor for pointing
            var rightCtrl = FindDeep("[BuildingBlock] Camera Rig", "RightControllerAnchor");
            if (rightCtrl != null)
            {
                ovrInput.rayTransform = rightCtrl.transform;
                results.AppendLine($"Set OVRInputModule.rayTransform → {rightCtrl.name}");
            }
            else
            {
                // Fallback: use CenterEyeAnchor for gaze-based input
                var centerEye = GameObject.Find("CenterEyeAnchor");
                if (centerEye != null)
                {
                    ovrInput.rayTransform = centerEye.transform;
                    results.AppendLine($"Set OVRInputModule.rayTransform → {centerEye.name} (fallback)");
                }
            }

            // Use right index trigger as click button for UI
            ovrInput.joyPadClickButton = OVRInput.Button.PrimaryIndexTrigger;

            // Start disabled — EditorInputFix enables the right module per platform
            ovrInput.enabled = false;
            results.AppendLine("OVRInputModule configured (starts disabled — EditorInputFix activates per platform)");

            EditorUtility.SetDirty(eventSystem);
        }
        else
        {
            results.AppendLine("WARNING: EventSystem not found");
        }

        // ── 3. Save ──
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }

    private static GameObject FindDeep(string rootName, string childName)
    {
        var root = GameObject.Find(rootName);
        if (root == null) return null;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            if (t.name == childName) return t.gameObject;
        }
        return null;
    }
}
