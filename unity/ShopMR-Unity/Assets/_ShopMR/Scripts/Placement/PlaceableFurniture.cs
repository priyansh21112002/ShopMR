using UnityEngine;
using ShopMR.Networking;

namespace ShopMR.Placement
{
    /// <summary>
    /// Per-instance behavior on a placed furniture object.
    /// Holds reference to source product and supports basic manipulation hooks.
    /// </summary>
    public class PlaceableFurniture : MonoBehaviour
    {
        [Header("Product Data")]
        public string productId;
        public string productName;
        public string category;
        public float price;

        [Header("State")]
        public bool isSelected;

        public void Initialize(ProductData product)
        {
            productId = product.product_id;
            productName = product.name;
            category = product.category;
            price = product.price;
            gameObject.name = $"Placed_{product.name}";

            // Do NOT override scale with product.dimensions —
            // prefabs are already authored at the correct size.
            // The dimensions field is metadata only (for UI display).
        }

        public void SetSelected(bool selected)
        {
            isSelected = selected;
            // Optional: visual feedback (outline, highlight) goes here in Stage 4
        }

        /// <summary>Rotate around world Y axis.</summary>
        public void Rotate(float degrees)
        {
            transform.Rotate(0, degrees, 0, Space.World);
        }

        /// <summary>Remove this placed furniture from the scene.</summary>
        public void Remove()
        {
            Destroy(gameObject);
        }
    }
}
