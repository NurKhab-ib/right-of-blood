using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public sealed class PrototypePlayerController : MonoBehaviour {
        public float Speed = 4f;
        public bool CanMove = true;
        public bool UseBounds = true;
        public Bounds MovementBounds;

        private void Update() {
            if (!CanMove || Keyboard.current == null) return;

            var movement = Vector2.zero;
            var keyboard = Keyboard.current;
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) movement.x -= 1f;
            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) movement.x += 1f;
            if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) movement.y -= 1f;
            if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) movement.y += 1f;
            if (movement.sqrMagnitude > 1f) movement.Normalize();

            var nextPosition = transform.position + (Vector3)(movement * Speed * Time.deltaTime);
            if (UseBounds) {
                nextPosition.x = Mathf.Clamp(nextPosition.x, MovementBounds.min.x + 0.3f, MovementBounds.max.x - 0.3f);
                nextPosition.y = Mathf.Clamp(nextPosition.y, MovementBounds.min.y + 0.4f, MovementBounds.max.y - 0.4f);
            }

            transform.position = nextPosition;
        }
    }
}