using UnityEngine;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixInputAndCanvas
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // === Fix 1: Add StandaloneInputModule to EventSystem ===
        var eventSystemGO = GameObject.Find("EventSystem");
        if (eventSystemGO != null)
        {
            var standalone = eventSystemGO.GetComponent<StandaloneInputModule>();
            if (standalone == null)
            {
                standalone = eventSystemGO.AddComponent<StandaloneInputModule>();
                results.AppendLine("Added StandaloneInputModule to EventSystem");
            }
            else
            {
                results.AppendLine("StandaloneInputModule already present");
            }

            // List all input modules for diagnostics
            var modules = eventSystemGO.GetComponents<BaseInputModule>();
            foreach (var m in modules)
            {
                results.AppendLine($"  InputModule: {m.GetType().Name} (enabled={m.enabled})");
            }

            EditorUtility.SetDirty(eventSystemGO);
        }
        else
        {
            results.AppendLine("ERROR: EventSystem not found");
        }

        // === Fix 2: Assign Event Camera on CatalogCanvas ===
        var catalogCanvas = GameObject.Find("CatalogCanvas");
        if (catalogCanvas != null)
        {
            var canvas = catalogCanvas.GetComponent<Canvas>();
            if (canvas != null)
            {
                if (canvas.worldCamera == null)
                {
                    // Find CenterEyeAnchor (MainCamera)
                    var mainCam = Camera.main;
                    if (mainCam != null)
                    {
                        canvas.worldCamera = mainCam;
                        results.AppendLine($"Assigned Event Camera: {mainCam.gameObject.name} (tag={mainCam.tag})");
                    }
                    else
                    {
                        // Try finding by path
                        var centerEye = GameObject.Find("CenterEyeAnchor");
                        if (centerEye != null)
                        {
                            var cam = centerEye.GetComponent<Camera>();
                            if (cam != null)
                            {
                                canvas.worldCamera = cam;
                                results.AppendLine($"Assigned Event Camera via path: CenterEyeAnchor");
                            }
                        }
                        else
                        {
                            results.AppendLine("WARNING: No camera found to assign as Event Camera");
                        }
                    }
                }
                else
                {
                    results.AppendLine($"Event Camera already assigned: {canvas.worldCamera.gameObject.name}");
                }
                EditorUtility.SetDirty(catalogCanvas);
            }
        }
        else
        {
            results.AppendLine("ERROR: CatalogCanvas not found");
        }

        // === Save ===
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved: {scene.path}");

        return results.ToString();
    }
}
