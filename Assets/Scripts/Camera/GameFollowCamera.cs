using UnityEngine;

namespace RightOfBlood.Prototype {
    [RequireComponent(typeof(Camera))]
    public sealed class GameFollowCamera : MonoBehaviour {
        [SerializeField] private PlayerController target;
        [SerializeField] private float orthographicSize = 3.2f;
        [SerializeField] private float smoothTime = 0.18f;
        [SerializeField] private Vector2 offset;
        [SerializeField] private bool clampToPlayerBounds = true;

        private Camera cameraComponent;
        private Vector3 velocity;

        private void Awake() {
            cameraComponent = GetComponent<Camera>();
            cameraComponent.orthographic = true;
            cameraComponent.orthographicSize = orthographicSize;
        }

        private void LateUpdate() {
            if (target == null) target = FindFirstObjectByType<PlayerController>();
            if (target == null) return;

            cameraComponent.orthographicSize = orthographicSize;

            var desiredPosition = target.transform.position + (Vector3)offset;
            desiredPosition.z = transform.position.z;

            if (clampToPlayerBounds && target.UseBounds && target.MovementBounds.size.sqrMagnitude > 0.01f) {
                desiredPosition = ClampToBounds(desiredPosition, target.MovementBounds);
            }

            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        }

        private Vector3 ClampToBounds(Vector3 desiredPosition, Bounds bounds) {
            var halfHeight = cameraComponent.orthographicSize;
            var halfWidth = halfHeight * cameraComponent.aspect;

            if (bounds.size.x <= halfWidth * 2f) {
                desiredPosition.x = bounds.center.x;
            }
            else {
                desiredPosition.x = Mathf.Clamp(desiredPosition.x, bounds.min.x + halfWidth, bounds.max.x - halfWidth);
            }

            if (bounds.size.y <= halfHeight * 2f) {
                desiredPosition.y = bounds.center.y;
            }
            else {
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, bounds.min.y + halfHeight, bounds.max.y - halfHeight);
            }

            return desiredPosition;
        }

        private void OnValidate() {
            orthographicSize = Mathf.Max(0.5f, orthographicSize);
            smoothTime = Mathf.Max(0.01f, smoothTime);
        }
    }
}