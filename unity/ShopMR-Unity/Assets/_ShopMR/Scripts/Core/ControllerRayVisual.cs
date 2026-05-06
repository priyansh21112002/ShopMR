using UnityEngine;

namespace ShopMR.Core
{
    /// <summary>
    /// Renders a visible laser ray from the right controller.
    /// Automatically finds the RightControllerAnchor at startup.
    /// Shows a small sphere dot where the ray hits a surface.
    /// </summary>
    public class ControllerRayVisual : MonoBehaviour
    {
        [Header("Ray Appearance")]
        [SerializeField] private Color rayColor = new Color(0.6f, 0.85f, 1f, 0.8f);
        [SerializeField] private Color rayHitUIColor = new Color(0.4f, 1f, 0.6f, 0.9f);
        [SerializeField] private float rayWidth = 0.003f;
        [SerializeField] private float maxRayLength = 10f;

        [Header("Hit Dot")]
        [SerializeField] private float dotScale = 0.015f;
        [SerializeField] private Color dotColor = new Color(1f, 1f, 1f, 0.9f);

        private Transform controllerTransform;
        private LineRenderer lineRenderer;
        private GameObject hitDot;
        private MeshRenderer dotRenderer;

        private void Start()
        {
            // Find right controller
            var cameraRig = FindObjectOfType<OVRCameraRig>();
            if (cameraRig != null && cameraRig.rightHandAnchor != null)
            {
                foreach (var t in cameraRig.rightHandAnchor.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name == "RightControllerAnchor")
                    {
                        controllerTransform = t;
                        break;
                    }
                }
            }

            if (controllerTransform == null)
            {
                // Fallback search
                var allTransforms = FindObjectsOfType<Transform>(true);
                foreach (var t in allTransforms)
                {
                    if (t.name == "RightControllerAnchor")
                    {
                        controllerTransform = t;
                        break;
                    }
                }
            }

            if (controllerTransform == null)
            {
                Debug.LogWarning("[ControllerRayVisual] RightControllerAnchor not found");
                enabled = false;
                return;
            }

            // Create LineRenderer for the ray
            var rayGO = new GameObject("ControllerRay");
            rayGO.transform.SetParent(transform);
            lineRenderer = rayGO.AddComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.startWidth = rayWidth;
            lineRenderer.endWidth = rayWidth * 0.5f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = rayColor;
            lineRenderer.endColor = rayColor;
            lineRenderer.receiveShadows = false;
            lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            lineRenderer.useWorldSpace = true;

            // Create hit dot
            hitDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            hitDot.name = "RayHitDot";
            hitDot.transform.SetParent(transform);
            hitDot.transform.localScale = Vector3.one * dotScale;
            Destroy(hitDot.GetComponent<Collider>()); // remove collider so it doesn't interfere

            var dotMat = new Material(Shader.Find("Sprites/Default"));
            dotMat.color = dotColor;
            dotRenderer = hitDot.GetComponent<MeshRenderer>();
            dotRenderer.material = dotMat;
            dotRenderer.receiveShadows = false;
            dotRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            hitDot.SetActive(false);

            Debug.Log("[ControllerRayVisual] Initialized with controller: " + controllerTransform.name);
        }

        private void LateUpdate()
        {
            if (controllerTransform == null || lineRenderer == null) return;

            Vector3 origin = controllerTransform.position;
            Vector3 direction = controllerTransform.forward;
            Vector3 endPoint = origin + direction * maxRayLength;
            bool hitSomething = false;

            // Raycast to find hit point
            if (Physics.Raycast(origin, direction, out RaycastHit hit, maxRayLength))
            {
                endPoint = hit.point;
                hitSomething = true;

                // Show dot at hit point
                hitDot.SetActive(true);
                hitDot.transform.position = hit.point + hit.normal * 0.001f;
                hitDot.transform.up = hit.normal;

                // Check if hitting UI (canvas collider)
                bool isUI = hit.collider.GetComponent<Canvas>() != null ||
                            hit.collider.GetComponentInParent<Canvas>() != null;
                Color currentColor = isUI ? rayHitUIColor : rayColor;
                lineRenderer.startColor = currentColor;
                lineRenderer.endColor = currentColor;
            }
            else
            {
                hitDot.SetActive(false);
                lineRenderer.startColor = rayColor;
                lineRenderer.endColor = new Color(rayColor.r, rayColor.g, rayColor.b, 0.2f);
            }

            lineRenderer.SetPosition(0, origin);
            lineRenderer.SetPosition(1, endPoint);
        }

        private void OnDisable()
        {
            if (lineRenderer != null) lineRenderer.enabled = false;
            if (hitDot != null) hitDot.SetActive(false);
        }

        private void OnEnable()
        {
            if (lineRenderer != null) lineRenderer.enabled = true;
        }
    }
}
