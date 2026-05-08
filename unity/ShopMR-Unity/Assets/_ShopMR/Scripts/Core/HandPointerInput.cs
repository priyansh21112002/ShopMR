using UnityEngine;
using UnityEngine.EventSystems;

namespace ShopMR.Core
{
    /// <summary>
    /// Enables hand tracking UI interaction on Meta Quest.
    /// Uses OVRHand's PointerPose for raycasting and index finger pinch for clicking.
    /// Automatically detects both hands and switches between controller/hand input.
    /// 
    /// Attach this to the same GameObject as the EventSystem.
    /// </summary>
    public class HandPointerInput : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float pinchThreshold = 0.7f;
        [SerializeField] private float maxRayDistance = 10f;

        private OVRHand leftHand;
        private OVRHand rightHand;
        private OVRCameraRig cameraRig;

        // Pinch state tracking
        private bool wasLeftPinching;
        private bool wasRightPinching;

        // Current pointer event data for each hand
        private PointerEventData leftPointerData;
        private PointerEventData rightPointerData;

        private void Start()
        {
            cameraRig = FindObjectOfType<OVRCameraRig>();
            FindHands();

            leftPointerData = new PointerEventData(EventSystem.current);
            rightPointerData = new PointerEventData(EventSystem.current);
        }

        private void FindHands()
        {
            if (cameraRig == null) return;

            var hands = cameraRig.GetComponentsInChildren<OVRHand>(true);
            foreach (var hand in hands)
            {
                if (hand.GetHand() == OVRPlugin.Hand.HandLeft)
                    leftHand = hand;
                else if (hand.GetHand() == OVRPlugin.Hand.HandRight)
                    rightHand = hand;
            }

            if (leftHand != null) Debug.Log("[HandPointerInput] Found left hand");
            if (rightHand != null) Debug.Log("[HandPointerInput] Found right hand");
        }

        private void Update()
        {
            if (leftHand == null || rightHand == null)
            {
                FindHands();
                if (leftHand == null && rightHand == null) return;
            }

            // Process each hand
            if (rightHand != null && rightHand.IsTracked && rightHand.IsPointerPoseValid)
            {
                ProcessHand(rightHand, ref wasRightPinching, rightPointerData);
            }

            if (leftHand != null && leftHand.IsTracked && leftHand.IsPointerPoseValid)
            {
                ProcessHand(leftHand, ref wasLeftPinching, leftPointerData);
            }
        }

        private void ProcessHand(OVRHand hand, ref bool wasPinching, PointerEventData pointerData)
        {
            Transform pointerPose = hand.PointerPose;
            if (pointerPose == null) return;

            bool isPinching = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index) > pinchThreshold;

            // Raycast from hand pointer pose
            Ray ray = new Ray(pointerPose.position, pointerPose.forward);
            pointerData.position = GetScreenPosition(ray);

            // Perform raycast through EventSystem
            var raycastResults = new System.Collections.Generic.List<RaycastResult>();
            EventSystem.current.RaycastAll(pointerData, raycastResults);

            RaycastResult closestResult = new RaycastResult();
            float closestDistance = float.MaxValue;
            foreach (var result in raycastResults)
            {
                if (result.distance < closestDistance)
                {
                    closestDistance = result.distance;
                    closestResult = result;
                }
            }
            pointerData.pointerCurrentRaycast = closestResult;

            GameObject currentTarget = closestResult.gameObject;

            // Handle pointer enter/exit
            HandlePointerEnterExit(pointerData, currentTarget);

            // Handle pinch down (click)
            if (isPinching && !wasPinching)
            {
                // Pinch started - pointer down
                if (currentTarget != null)
                {
                    pointerData.pressPosition = pointerData.position;
                    pointerData.pointerPressRaycast = closestResult;
                    pointerData.eligibleForClick = true;

                    // Find the press handler
                    var pressed = ExecuteEvents.ExecuteHierarchy(
                        currentTarget, pointerData, ExecuteEvents.pointerDownHandler);

                    if (pressed == null)
                        pressed = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentTarget);

                    pointerData.pointerPress = pressed;
                    pointerData.rawPointerPress = currentTarget;

                    // Also select the object (important for input fields)
                    var selectable = ExecuteEvents.GetEventHandler<ISelectHandler>(currentTarget);
                    if (selectable != null && selectable != EventSystem.current.currentSelectedGameObject)
                    {
                        EventSystem.current.SetSelectedGameObject(selectable, pointerData);
                    }
                }
            }
            else if (!isPinching && wasPinching)
            {
                // Pinch released - pointer up + click
                if (pointerData.pointerPress != null)
                {
                    ExecuteEvents.Execute(
                        pointerData.pointerPress, pointerData, ExecuteEvents.pointerUpHandler);

                    // If still over the same target, fire click
                    if (currentTarget != null)
                    {
                        var clickHandler = ExecuteEvents.GetEventHandler<IPointerClickHandler>(currentTarget);
                        if (clickHandler == pointerData.pointerPress)
                        {
                            ExecuteEvents.Execute(
                                pointerData.pointerPress, pointerData, ExecuteEvents.pointerClickHandler);
                        }
                    }
                }

                pointerData.pointerPress = null;
                pointerData.rawPointerPress = null;
                pointerData.eligibleForClick = false;
            }

            wasPinching = isPinching;
        }

        private void HandlePointerEnterExit(PointerEventData pointerData, GameObject currentTarget)
        {
            if (currentTarget != pointerData.pointerEnter)
            {
                // Exit old target
                if (pointerData.pointerEnter != null)
                {
                    ExecuteEvents.Execute(
                        pointerData.pointerEnter, pointerData, ExecuteEvents.pointerExitHandler);
                }

                // Enter new target
                pointerData.pointerEnter = currentTarget;
                if (currentTarget != null)
                {
                    ExecuteEvents.Execute(
                        currentTarget, pointerData, ExecuteEvents.pointerEnterHandler);
                }
            }
        }

        private Vector2 GetScreenPosition(Ray ray)
        {
            // Convert the 3D ray to a screen position for the EventSystem
            if (Camera.main != null)
            {
                // Project the ray hit point (or a point along the ray) to screen space
                Vector3 worldPoint = ray.origin + ray.direction * 1f;
                Vector3 screenPos = Camera.main.WorldToScreenPoint(worldPoint);
                return new Vector2(screenPos.x, screenPos.y);
            }
            return new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        }
    }
}
