using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

public class FixPassthroughCameras
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Fix LeftEyeAnchor camera
        var leftEye = GameObject.Find("LeftEyeAnchor");
        if (leftEye != null)
        {
            var cam = leftEye.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                EditorUtility.SetDirty(leftEye);
                results.AppendLine($"Fixed LeftEyeAnchor camera: clearFlags=SolidColor, bg=(0,0,0,0)");
            }
        }
        else
        {
            results.AppendLine("WARNING: LeftEyeAnchor not found");
        }

        // 2. Fix RightEyeAnchor camera
        var rightEye = GameObject.Find("RightEyeAnchor");
        if (rightEye != null)
        {
            var cam = rightEye.GetComponent<Camera>();
            if (cam != null)
            {
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                EditorUtility.SetDirty(rightEye);
                results.AppendLine($"Fixed RightEyeAnchor camera: clearFlags=SolidColor, bg=(0,0,0,0)");
            }
        }
        else
        {
            results.AppendLine("WARNING: RightEyeAnchor not found");
        }

        // 3. Verify CenterEyeAnchor is also correct
        var centerEye = GameObject.Find("CenterEyeAnchor");
        if (centerEye != null)
        {
            var cam = centerEye.GetComponent<Camera>();
            if (cam != null)
            {
                bool wasCorrect = cam.clearFlags == CameraClearFlags.SolidColor
                    && cam.backgroundColor == new Color(0f, 0f, 0f, 0f);
                if (!wasCorrect)
                {
                    cam.clearFlags = CameraClearFlags.SolidColor;
                    cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
                    EditorUtility.SetDirty(centerEye);
                    results.AppendLine("Fixed CenterEyeAnchor camera too");
                }
                else
                {
                    results.AppendLine("CenterEyeAnchor already correct (SolidColor, 0,0,0,0)");
                }
            }
        }

        // 4. Remove skybox from RenderSettings so it can't leak into passthrough
        if (RenderSettings.skybox != null)
        {
            string skyboxName = RenderSettings.skybox.name;
            RenderSettings.skybox = null;
            results.AppendLine($"Removed skybox material '{skyboxName}' from RenderSettings");
        }
        else
        {
            results.AppendLine("RenderSettings skybox already null");
        }

        // 5. Set ambient mode to flat color (no skybox contribution)
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.5f, 0.5f, 0.5f, 1f);
        results.AppendLine("Set ambient mode to Flat with neutral grey ambient light");

        // 6. Save the scene
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Saved scene: {scene.path}");

        return results.ToString();
    }
}
