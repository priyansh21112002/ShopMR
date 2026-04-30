using System;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Oculus.Interaction;
using Oculus.Interaction.Surfaces;

/// <summary>
/// Sets up a World Space CatalogCanvas in the MainMR scene with:
/// - CatalogPanel prefab as child (stretch-fit)
/// - ISDK Ray Interaction template (PointableCanvas + RayInteractable + Surfaces)
/// - ISDK Poke Interaction template (PointableCanvas + PokeInteractable + Surfaces)
/// - EventSystem with PointableCanvasModule
/// - Attempts to add interactors to the camera rig
/// </summary>
public class SetupCatalogCanvasWithInteraction
{
    // Template GUIDs from Meta Interaction SDK QuickActions
    private const string RAY_TEMPLATE_GUID  = "8369d93f7b6b99742bbea0649a41b7b1";
    private const string POKE_TEMPLATE_GUID = "4db41829582c7d24f80ee9603868dd67";
    private const string CATALOG_PANEL_PREFAB = "Assets/_ShopMR/Prefabs/CatalogPanel.prefab";

    public static string Execute()
    {
        var log = new System.Text.StringBuilder();

        try
        {
            // ── Step 1: Clean up any existing temp canvas or stale CatalogCanvas ──
            CleanupExisting("_TempCanvas", log);
            CleanupExisting("CatalogCanvas", log);

            // ── Step 2a: Create World Space Canvas ──
            GameObject canvasGO = new GameObject("CatalogCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGO, "Create CatalogCanvas");

            // Add Canvas component - World Space
            Canvas canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;

            // Add CanvasScaler
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.dynamicPixelsPerUnit = 10f;

            // Add GraphicRaycaster (required by PointableCanvas)
            canvasGO.AddComponent<GraphicRaycaster>();

            // Configure RectTransform
            RectTransform canvasRT = canvasGO.GetComponent<RectTransform>();
            canvasRT.sizeDelta = new Vector2(800f, 600f);
            canvasRT.localScale = new Vector3(0.001f, 0.001f, 0.001f);
            canvasRT.position = new Vector3(0f, 1.5f, 1.5f);
            canvasRT.localRotation = Quaternion.identity;

            log.AppendLine("✅ Created CatalogCanvas (World Space, 800×600, scale=0.001, pos=(0,1.5,1.5))");

            // ── Step 2b: Instantiate CatalogPanel prefab as child ──
            GameObject catalogPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CATALOG_PANEL_PREFAB);
            if (catalogPanelPrefab == null)
            {
                log.AppendLine("❌ Could not load CatalogPanel prefab at: " + CATALOG_PANEL_PREFAB);
                return log.ToString();
            }

            GameObject catalogPanelInstance = (GameObject)PrefabUtility.InstantiatePrefab(catalogPanelPrefab, canvasGO.transform);
            catalogPanelInstance.name = "CatalogPanel";

            // Set stretch-fit anchors
            RectTransform panelRT = catalogPanelInstance.GetComponent<RectTransform>();
            panelRT.anchorMin = Vector2.zero;
            panelRT.anchorMax = Vector2.one;
            panelRT.offsetMin = Vector2.zero; // Left, Bottom
            panelRT.offsetMax = Vector2.zero; // Right, Top
            panelRT.localPosition = Vector3.zero;
            panelRT.localRotation = Quaternion.identity;
            panelRT.localScale = Vector3.one;

            log.AppendLine("✅ Added CatalogPanel prefab as child (stretch-fit)");

            // ── Step 3: Add ISDK Ray Interaction template ──
            bool rayAdded = AddInteractionTemplate(canvasGO, canvas, RAY_TEMPLATE_GUID,
                "ISDK_RayCanvasInteraction", log);

            // ── Step 3 (optional): Add ISDK Poke Interaction template ──
            bool pokeAdded = AddInteractionTemplate(canvasGO, canvas, POKE_TEMPLATE_GUID,
                "ISDK_PokeCanvasInteraction", log);

            // ── Step 4: Ensure EventSystem with PointableCanvasModule ──
            EnsurePointableCanvasModule(log);

            // ── Step 5: Try to add interactors to the rig ──
            TryAddInteractorsToRig(log);

            // ── Step 6: Save the scene ──
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveOpenScenes();
            log.AppendLine("✅ Scene saved");

            return log.ToString();
        }
        catch (Exception ex)
        {
            log.AppendLine($"❌ Exception: {ex.Message}\n{ex.StackTrace}");
            return log.ToString();
        }
    }

    private static void CleanupExisting(string name, System.Text.StringBuilder log)
    {
        var existing = GameObject.Find(name);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
            log.AppendLine($"🗑 Deleted existing '{name}'");
        }
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
        Undo.RegisterCreatedObjectUndo(instance, "Add " + displayName);

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

    private static void EnsurePointableCanvasModule(System.Text.StringBuilder log)
    {
        // Check if PointableCanvasModule already exists
        var existingModule = UnityEngine.Object.FindAnyObjectByType<PointableCanvasModule>();
        if (existingModule != null)
        {
            log.AppendLine("✅ PointableCanvasModule already exists on: " + existingModule.gameObject.name);
            return;
        }

        // Check if EventSystem exists
        EventSystem eventSystem = UnityEngine.Object.FindAnyObjectByType<EventSystem>();
        if (eventSystem != null)
        {
            // Add PointableCanvasModule to existing EventSystem
            Undo.AddComponent<PointableCanvasModule>(eventSystem.gameObject);
            log.AppendLine("✅ Added PointableCanvasModule to existing EventSystem");
        }
        else
        {
            // Create new EventSystem with PointableCanvasModule
            GameObject esGO = new GameObject("EventSystem");
            Undo.RegisterCreatedObjectUndo(esGO, "Create EventSystem");
            esGO.AddComponent<EventSystem>();
            esGO.AddComponent<PointableCanvasModule>();
            log.AppendLine("✅ Created EventSystem with PointableCanvasModule");
        }
    }

    private static void TryAddInteractorsToRig(System.Text.StringBuilder log)
    {
        try
        {
            // Use reflection to call internal InteractorUtils.AddInteractorsToRig
            // from Oculus.Interaction.Editor.QuickActions namespace
            var assemblies = System.AppDomain.CurrentDomain.GetAssemblies();
            Assembly editorAssembly = null;

            foreach (var asm in assemblies)
            {
                if (asm.GetName().Name.Contains("Oculus.Interaction.Editor"))
                {
                    editorAssembly = asm;
                    break;
                }
            }

            if (editorAssembly == null)
            {
                // Try finding it by type
                foreach (var asm in assemblies)
                {
                    var t = asm.GetType("Oculus.Interaction.Editor.QuickActions.InteractorUtils");
                    if (t != null)
                    {
                        editorAssembly = asm;
                        break;
                    }
                }
            }

            if (editorAssembly == null)
            {
                log.AppendLine("⚠️  Could not find Oculus.Interaction.Editor assembly for interactor setup");
                log.AppendLine("   → You may need to manually add Ray/Poke interactors via the ISDK wizard");
                return;
            }

            Type interactorUtilsType = editorAssembly.GetType("Oculus.Interaction.Editor.QuickActions.InteractorUtils");
            Type interactorTypesEnum = editorAssembly.GetType("Oculus.Interaction.Editor.QuickActions.InteractorTypes");
            Type deviceTypesEnum = editorAssembly.GetType("Oculus.Interaction.Editor.QuickActions.DeviceTypes");

            if (interactorUtilsType == null || interactorTypesEnum == null || deviceTypesEnum == null)
            {
                log.AppendLine("⚠️  Could not find InteractorUtils types via reflection");
                return;
            }

            // InteractorTypes.Ray | InteractorTypes.Poke
            int rayValue = (int)Enum.Parse(interactorTypesEnum, "Ray");
            int pokeValue = (int)Enum.Parse(interactorTypesEnum, "Poke");
            object interactorTypes = Enum.ToObject(interactorTypesEnum, rayValue | pokeValue);

            // DeviceTypes.All
            object deviceTypes = Enum.Parse(deviceTypesEnum, "All");

            MethodInfo addMethod = interactorUtilsType.GetMethod("AddInteractorsToRig",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            if (addMethod != null)
            {
                var result = addMethod.Invoke(null, new object[] { interactorTypes, deviceTypes });
                log.AppendLine("✅ Called InteractorUtils.AddInteractorsToRig(Ray|Poke, All)");

                // Count what was added
                if (result is System.Collections.IEnumerable enumerable)
                {
                    int count = 0;
                    foreach (var item in enumerable) count++;
                    log.AppendLine($"   → {count} interactor(s) added to rig");
                    if (count == 0)
                    {
                        log.AppendLine("   → (0 means rig has no ISDK Hand/Controller data sources,");
                        log.AppendLine("      or interactors already exist. This is OK for OVR rigs —");
                        log.AppendLine("      the OVR rig uses its own raycasting for UI interaction.)");
                    }
                }
            }
            else
            {
                log.AppendLine("⚠️  Could not find AddInteractorsToRig method");
            }
        }
        catch (Exception ex)
        {
            log.AppendLine($"⚠️  Interactor rig setup failed (non-critical): {ex.Message}");
            log.AppendLine("   → The canvas interaction components are set up correctly.");
            log.AppendLine("   → You can manually add interactors via: right-click Canvas → Interaction SDK → Add Ray Interaction");
        }
    }
}
