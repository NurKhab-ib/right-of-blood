using UnityEngine;

namespace RightOfBlood.Prototype {
    public sealed class Interactable : MonoBehaviour {
        [SerializeField] private string label = "Interact";
        [SerializeField] private PrototypeInteractionKind kind;
        [SerializeField] private LocationId targetLocation;
        [SerializeField] private string targetSpawnId = "default";

        private bool scenePlacementCaptured;
        private Transform sceneParent;
        private Vector3 sceneLocalPosition;
        private Quaternion sceneLocalRotation;
        private Vector3 sceneLocalScale;

        public string Label => string.IsNullOrWhiteSpace(label) ? name : label;
        public PrototypeInteractionKind Kind => kind;
        public LocationId TargetLocation => targetLocation;
        public string TargetSpawnId => string.IsNullOrWhiteSpace(targetSpawnId) ? "default" : targetSpawnId;

        public void CaptureScenePlacement() {
            if (scenePlacementCaptured) return;

            scenePlacementCaptured = true;
            sceneParent = transform.parent;
            sceneLocalPosition = transform.localPosition;
            sceneLocalRotation = transform.localRotation;
            sceneLocalScale = transform.localScale;
        }

        public bool ScenePlacementBelongsTo(Location location) {
            if (!scenePlacementCaptured || location == null) return false;

            var parent = sceneParent;
            while (parent != null) {
                if (parent == location.transform) return true;
                parent = parent.parent;
            }

            return false;
        }

        public void MoveToScenePlacement() {
            if (!scenePlacementCaptured) CaptureScenePlacement();

            transform.SetParent(sceneParent, false);
            transform.localPosition = sceneLocalPosition;
            transform.localRotation = sceneLocalRotation;
            transform.localScale = sceneLocalScale;
            gameObject.SetActive(true);
        }

        public void MoveToLocation(Location location, string spawnId, Vector2 localOffset) {
            if (!scenePlacementCaptured) CaptureScenePlacement();
            if (location == null) {
                gameObject.SetActive(false);
                return;
            }

            transform.SetParent(location.transform, false);
            var spawn = location.GetSpawn(spawnId);
            transform.position = spawn != null ? spawn.position + (Vector3)localOffset : location.transform.position + (Vector3)localOffset;
            transform.localRotation = sceneLocalRotation;
            transform.localScale = sceneLocalScale;
            gameObject.SetActive(true);
        }

        public void SetVisible(bool visible) {
            gameObject.SetActive(visible);
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, QuestGame.DefaultInteractionRange);
        }
    }
}