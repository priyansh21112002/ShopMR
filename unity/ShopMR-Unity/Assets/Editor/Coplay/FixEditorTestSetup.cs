using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Coplay.Controllers.Functions;

public class FixEditorTestSetup
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Move Camera Rig to head height for editor testing
        // On Quest, OVR tracking overrides this position, so it's safe
        var cameraRig = GameObject.Find("[BuildingBlock] Camera Rig");
        if (cameraRig != null)
        {
            cameraRig.transform.position = new Vector3(0f, 1.6f, 0f);
            EditorUtility.SetDirty(cameraRig);
            results.AppendLine($"Moved Camera Rig to (0, 1.6, 0) — simulates standing eye height");
        }
        else
        {
            results.AppendLine("WARNING: Camera Rig not found");
        }

        // 2. Verify _EditorTestFloor exists and position it correctly
        var floor = GameObject.Find("_EditorTestFloor");
        if (floor != null)
        {
            // Floor plane at y=0, centered ahead of camera at z=2
            // The plane extends 20x20m which is plenty
            floor.transform.position = new Vector3(0f, 0f, 2f);
            floor.transform.localScale = new Vector3(2f, 1f, 2f);
            EditorUtility.SetDirty(floor);
            results.AppendLine($"Floor verified at {floor.transform.position}, scale {floor.transform.localScale}");
        }
        else
        {
            results.AppendLine("WARNING: _EditorTestFloor not found");
        }

        // 3. Verify camera can see the floor now
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 camPos = cam.transform.position;
            // After rig move, cam should be at (0, 1.6, 0)
            // Floor surface is at y=0 extending around (0, 0, 2)
            // Camera looking forward (+Z) at y=1.6 should see ground plane below
            results.AppendLine($"Camera position after rig move: {camPos}");
            results.AppendLine($"Camera will look forward and downward to see floor at y=0, z=[-8..12]");

            // Do a test raycast from camera center
            Ray testRay = new Ray(camPos, cam.transform.forward);
            if (Physics.Raycast(testRay, out RaycastHit hit, 20f))
            {
                results.AppendLine($"Test raycast (forward) hit: {hit.collider.gameObject.name} at {hit.point}");
            }
            else
            {
                // Try downward-forward
                Vector3 dir = (cam.transform.forward + Vector3.down * 0.5f).normalized;
                Ray testRay2 = new Ray(camPos, dir);
                if (Physics.Raycast(testRay2, out RaycastHit hit2, 20f))
                {
                    results.AppendLine($"Test raycast (forward-down) hit: {hit2.collider.gameObject.name} at {hit2.point}");
                }
                else
                {
                    results.AppendLine("WARNING: Test raycast missed — mouse-based raycast should still work since it casts from screen point");
                }
            }
        }

        // 4. Save the scene to the correct path
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        results.AppendLine($"Active scene path: {scene.path}");
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        results.AppendLine($"Scene saved to: {scene.path}");

        return results.ToString();
    }
}
