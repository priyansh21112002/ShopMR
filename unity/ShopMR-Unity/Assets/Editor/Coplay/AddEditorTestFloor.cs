using UnityEngine;
using UnityEditor;
using Coplay.Controllers.Functions;

public class AddEditorTestFloor
{
    public static string Execute()
    {
        var results = new System.Text.StringBuilder();

        // 1. Create the test floor plane
        var floorResult = CoplayTools.CreateGameObject(
            "_EditorTestFloor",
            "0,0,2",
            primitiveType: "Plane",
            size: "2,1,2"
        );
        results.AppendLine($"Floor creation: {floorResult}");

        // 2. Create a dark grey material so it's visible
        var matResult = CoplayTools.CreateMaterial(
            "EditorFloorMat",
            "0.25,0.25,0.3,1",
            "Assets/_ShopMR/Materials"
        );
        results.AppendLine($"Material creation: {matResult}");

        // 3. Assign material to floor
        var assignResult = CoplayTools.AssignMaterial(
            "_EditorTestFloor",
            "EditorFloorMat",
            "Assets/_ShopMR/Materials"
        );
        results.AppendLine($"Material assign: {assignResult}");

        // 4. Check camera position
        var cam = Camera.main;
        if (cam != null)
        {
            results.AppendLine($"Camera.main found: {cam.gameObject.name} at position {cam.transform.position}, tag={cam.tag}");
            results.AppendLine($"Camera forward: {cam.transform.forward}");
        }
        else
        {
            results.AppendLine("WARNING: Camera.main is null — no camera tagged MainCamera");
            // Find any camera
            var allCams = Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
            foreach (var c in allCams)
            {
                results.AppendLine($"  Found camera: {c.gameObject.name} at {c.transform.position}, tag={c.tag}, enabled={c.enabled}");
            }
        }

        // 5. Save scene
        var saveResult = CoplayTools.SaveScene("MainMR");
        results.AppendLine($"Scene save: {saveResult}");

        return results.ToString();
    }
}
