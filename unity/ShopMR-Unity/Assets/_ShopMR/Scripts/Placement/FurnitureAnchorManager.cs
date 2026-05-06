using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShopMR.Placement
{
    /// <summary>
    /// Manages OVRSpatialAnchors for placed furniture so that objects stay
    /// locked to their physical positions during tracking re-localization
    /// (e.g. walking out of the room and back in).
    /// 
    /// No cross-session persistence — anchors are only valid for the current app session.
    /// </summary>
    public class FurnitureAnchorManager : MonoBehaviour
    {
        public static FurnitureAnchorManager Instance { get; private set; }

        // Runtime tracking of anchored objects
        private Dictionary<Guid, GameObject> anchoredObjects = new Dictionary<Guid, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// Attach a spatial anchor to a placed furniture object.
        /// Call this right after ConfirmPlacement in FurniturePlacer.
        /// </summary>
        public async void AnchorFurniture(GameObject placedObject, string productId, string productName, string modelUrl, string category)
        {
#if UNITY_EDITOR
            // Spatial anchors don't work in Editor — just log
            Debug.Log($"[FurnitureAnchorManager] (Editor) Would anchor: {productName}");
            return;
#else
            if (placedObject == null) return;

            // Add OVRSpatialAnchor component — this triggers anchor creation at the object's current pose
            var anchor = placedObject.AddComponent<OVRSpatialAnchor>();

            // Wait for the anchor to be created and localized
            bool localized = await anchor.WhenLocalizedAsync();
            if (!localized || anchor == null)
            {
                Debug.LogWarning($"[FurnitureAnchorManager] Failed to localize anchor for {productName}");
                return;
            }

            Debug.Log($"[FurnitureAnchorManager] Anchor localized: {anchor.Uuid} for {productName}");

            // Track in runtime dictionary
            anchoredObjects[anchor.Uuid] = placedObject;
#endif
        }

        /// <summary>
        /// Remove an anchored item (when user deletes placed furniture).
        /// </summary>
        public void RemoveAnchor(GameObject placedObject)
        {
#if UNITY_EDITOR
            return;
#else
            if (placedObject == null) return;
            var anchor = placedObject.GetComponent<OVRSpatialAnchor>();
            if (anchor == null || !anchor.Created) return;

            Guid uuid = anchor.Uuid;
            anchoredObjects.Remove(uuid);

            Debug.Log($"[FurnitureAnchorManager] Anchor removed: {uuid}");
#endif
        }

        /// <summary>
        /// Remove all anchors.
        /// </summary>
        public void RemoveAllAnchors()
        {
            anchoredObjects.Clear();
            Debug.Log("[FurnitureAnchorManager] All anchors removed.");
        }
    }
}
