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
        private string runtimeLabelOverride;

        public string Label => TextNormalizer.Normalize(string.IsNullOrWhiteSpace(runtimeLabelOverride) ? (string.IsNullOrWhiteSpace(label) ? name : label) : runtimeLabelOverride);
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

        public void ConfigureRuntime(string newLabel, PrototypeInteractionKind newKind) {
            label = newLabel;
            kind = newKind;
            runtimeLabelOverride = null;
        }

        public void SetVisible(bool visible) {
            gameObject.SetActive(visible);
        }

        public void SetLabel(string value) {
var normalized = TextNormalizer.Normalize(value);
            runtimeLabelOverride = string.IsNullOrWhiteSpace(normalized) ? null :
                (normalized.IndexOf('?') >= 0 ? FallbackLabel() : normalized);
        }

        private string FallbackLabel() {
            switch (kind) {
                case PrototypeInteractionKind.missing_document_desk: return "\u0420\u0430\u0431\u043e\u0447\u0438\u0439 \u0441\u0442\u043e\u043b";
                case PrototypeInteractionKind.chief: return "\u041d\u0430\u0447\u0430\u043b\u044c\u043d\u0438\u043a \u043e\u0442\u0434\u0435\u043b\u0430";
                case PrototypeInteractionKind.archive_security:
                case PrototypeInteractionKind.guard_post: return "\u041e\u0445\u0440\u0430\u043d\u0430 \u0430\u0440\u0445\u0438\u0432\u0430";
                case PrototypeInteractionKind.council_scholar: return "\u0423\u0447\u0451\u043d\u044b\u0439 \u0421\u043e\u0432\u0435\u0442\u0430";
                case PrototypeInteractionKind.mafia_fixer: return "\u041f\u043e\u0441\u0440\u0435\u0434\u043d\u0438\u043a \u041c\u0430\u0444\u0438\u0438";
                case PrototypeInteractionKind.former_archivist: return "\u0411\u044b\u0432\u0448\u0438\u0439 \u0430\u0440\u0445\u0438\u0432\u0430\u0440\u0438\u0443\u0441";
                case PrototypeInteractionKind.archive_shelf: return "\u0410\u0440\u0445\u0438\u0432\u043d\u044b\u0435 \u043f\u043e\u043b\u043a\u0438";
                case PrototypeInteractionKind.council_public_library: return "\u041f\u0443\u0431\u043b\u0438\u0447\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430";
                case PrototypeInteractionKind.council_secret_library: return "\u0422\u0430\u0439\u043d\u0430\u044f \u0431\u0438\u0431\u043b\u0438\u043e\u0442\u0435\u043a\u0430";
                case PrototypeInteractionKind.black_archive_door: return "\u0427\u0451\u0440\u043d\u044b\u0439 \u0445\u043e\u0434 \u0430\u0440\u0445\u0438\u0432\u0430";
                case PrototypeInteractionKind.street_patrol: return "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u043f\u0430\u0442\u0440\u0443\u043b\u044c";
                case PrototypeInteractionKind.street_raid: return "\u0413\u043e\u0440\u043e\u0434\u0441\u043a\u043e\u0439 \u0440\u0435\u0439\u0434";
                case PrototypeInteractionKind.door: return "\u041f\u0440\u043e\u0445\u043e\u0434";
                default: return "\u0418\u043d\u0442\u0435\u0440\u0430\u043a\u0442\u0438\u0432\u043d\u044b\u0439 \u043e\u0431\u044a\u0435\u043a\u0442";
            }
        }
        public void ClearLabelOverride() {
            runtimeLabelOverride = null;
        }

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, QuestGame.DefaultInteractionRange);
        }
    }
}
