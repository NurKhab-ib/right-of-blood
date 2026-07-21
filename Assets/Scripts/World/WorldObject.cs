using UnityEngine;

namespace RightOfBlood.Prototype {
    [DisallowMultipleComponent]
    public sealed class WorldObject : MonoBehaviour {
        [SerializeField] private WorldObjectState state = WorldObjectState.available;
        [SerializeField] private Vector2 npcColliderSize = new Vector2(0.55f, 0.7f);

        public WorldObjectState State => state;

        private void Awake() {
            SyncCollision();
        }

        public void SetState(WorldObjectState nextState) {
            state = nextState;
            SyncCollision();
        }

        public static bool IsNpc(PrototypeInteractionKind kind) {
            switch (kind) {
                case PrototypeInteractionKind.chief:
                case PrototypeInteractionKind.archive_security:
                case PrototypeInteractionKind.council_scholar:
                case PrototypeInteractionKind.mafia_fixer:
                case PrototypeInteractionKind.former_archivist:
                case PrototypeInteractionKind.archive_investigator:
                case PrototypeInteractionKind.street_patrol:
                case PrototypeInteractionKind.guard_post:
                case PrototypeInteractionKind.street_raid:
                    return true;
                default:
                    return false;
            }
        }

        private void SyncCollision() {
            var interactable = GetComponent<Interactable>();
            if (interactable == null || !IsNpc(interactable.Kind)) {
                foreach (var collider in GetComponents<Collider2D>()) Destroy(collider);
                return;
            }

            var collider2D = GetComponent<BoxCollider2D>();
            if (collider2D == null) collider2D = gameObject.AddComponent<BoxCollider2D>();
            collider2D.isTrigger = false;
            collider2D.offset = new Vector2(0f, -0.1f);
            collider2D.size = npcColliderSize;
            collider2D.enabled = state != WorldObjectState.hidden && state != WorldObjectState.destroyed;
        }
    }
}