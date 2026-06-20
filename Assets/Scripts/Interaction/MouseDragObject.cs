using UnityEngine;
using UnityEngine.InputSystem;
using Laboratory.FunnelSystem;
using Laboratory.FilterPaperSystem;

namespace Laboratory.Interaction
{
    /// <summary>
    /// Click and drag a collider on a horizontal plane.
    /// When used on Filter Paper, release near a Funnel to snap and attach.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public sealed class MouseDragObject : MonoBehaviour
    {
        [SerializeField] private Transform dragTarget;
        [SerializeField, Min(0.1f)] private float filterSnapDistance = 1f;

        private Collider interactionCollider;
        private Camera sceneCamera;
        private Plane dragPlane;
        private Vector3 dragOffset;
        private bool isDragging;
        private FilterPaperController filterPaper;

        public bool IsDragging => isDragging;

        private void Awake()
        {
            interactionCollider = GetComponent<Collider>();
            sceneCamera = Camera.main;

            if (dragTarget == null)
                dragTarget = transform.parent != null ? transform.parent : transform;

            filterPaper = dragTarget.GetComponent<FilterPaperController>();
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
                return;

            Vector2 pointerPosition = mouse.position.ReadValue();

            if (mouse.leftButton.wasPressedThisFrame)
                TryBeginDrag(pointerPosition);

            if (isDragging && mouse.leftButton.isPressed)
                ContinueDrag(pointerPosition);

            if (isDragging && mouse.leftButton.wasReleasedThisFrame)
                EndDrag();
        }

        private void TryBeginDrag(Vector2 pointerPosition)
        {
            if (sceneCamera == null)
                sceneCamera = Camera.main;

            if (sceneCamera == null)
                return;

            Ray pointerRay = sceneCamera.ScreenPointToRay(pointerPosition);
            if (!Physics.Raycast(pointerRay, out RaycastHit hit) ||
                hit.collider != interactionCollider)
            {
                return;
            }

            if (filterPaper != null)
            {
                filterPaper.OnDetachedFromFunnel();
                dragTarget.SetParent(null, true);
            }

            dragPlane = new Plane(Vector3.up, dragTarget.position);
            if (!dragPlane.Raycast(pointerRay, out float distance))
                return;

            dragOffset =
                dragTarget.position - pointerRay.GetPoint(distance);
            isDragging = true;
        }

        private void ContinueDrag(Vector2 pointerPosition)
        {
            Ray pointerRay = sceneCamera.ScreenPointToRay(pointerPosition);
            if (dragPlane.Raycast(pointerRay, out float distance))
            {
                dragTarget.position =
                    pointerRay.GetPoint(distance) + dragOffset;
            }
        }

        private void EndDrag()
        {
            isDragging = false;

            if (filterPaper == null)
                return;

            FunnelController nearestFunnel = FindNearestFunnel();
            if (nearestFunnel != null)
                filterPaper.OnSnappedToFunnel(nearestFunnel);
        }

        private FunnelController FindNearestFunnel()
        {
            FunnelController nearest = null;
            float nearestDistance = filterSnapDistance;

            foreach (FunnelController funnel in
                     Object.FindObjectsByType<FunnelController>())
            {
                float distance = Vector3.Distance(
                    dragTarget.position,
                    funnel.transform.position);

                if (distance <= nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = funnel;
                }
            }

            return nearest;
        }
    }
}
