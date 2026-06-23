using UnityEngine;

namespace RightOfBlood.Prototype {
    public sealed class PrototypeInteractable : MonoBehaviour {
        [SerializeField] private string label = "Interact";
        [SerializeField] private PrototypeInteractionKind kind;
        [SerializeField] private LocationId targetLocation;
        [SerializeField] private string targetSpawnId = "default";

        public string Label => string.IsNullOrWhiteSpace(label) ? name : label;
        public PrototypeInteractionKind Kind => kind;
        public LocationId TargetLocation => targetLocation;
        public string TargetSpawnId => string.IsNullOrWhiteSpace(targetSpawnId) ? "default" : targetSpawnId;

        private void OnDrawGizmosSelected() {
            Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.45f);
            Gizmos.DrawWireSphere(transform.position, IntroQuestGame.DefaultInteractionRange);
        }
    }
}