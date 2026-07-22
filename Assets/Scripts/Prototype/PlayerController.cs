using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public sealed class PlayerController : MonoBehaviour {
        public float Speed = 6f;
        public float SprintMultiplier = 1.75f;
        public bool CanMove = true;
        public bool UseBounds = true;
        public Bounds MovementBounds;

        private Rigidbody2D body;
        private Vector2 requestedVelocity;

        private void Awake() {
            body = GetComponent<Rigidbody2D>();
            if (body == null) body = gameObject.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            var collider = GetComponent<CircleCollider2D>();
            if (collider == null) collider = gameObject.AddComponent<CircleCollider2D>();
            collider.radius = 0.35f;
        }

        private void Update() {
            if (!CanMove || GameConsole.IsInputBlocked || Keyboard.current == null) {
                requestedVelocity = Vector2.zero;
                return;
            }

            var movement = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
            if (movement.sqrMagnitude > 1f) movement.Normalize();

            var sprintMultiplier = keyboard.leftShiftKey.isPressed || keyboard.rightShiftKey.isPressed ? SprintMultiplier : 1f;
            requestedVelocity = movement * (Speed * sprintMultiplier);
        }

        private void FixedUpdate() {
            if (body == null) return;
            body.linearVelocity = CanMove && !GameConsole.IsInputBlocked ? requestedVelocity : Vector2.zero;

            if (UseBounds && MovementBounds.size.sqrMagnitude > 0.01f) {
                var position = body.position;
                position.x = Mathf.Clamp(position.x, MovementBounds.min.x + 0.3f, MovementBounds.max.x - 0.3f);
                position.y = Mathf.Clamp(position.y, MovementBounds.min.y + 0.4f, MovementBounds.max.y - 0.4f);
                body.position = position;
            }
        }

        public void StopImmediately() {
            requestedVelocity = Vector2.zero;
            if (body != null) body.linearVelocity = Vector2.zero;
        }

        private void OnDisable() {
            StopImmediately();
        }
    }
}