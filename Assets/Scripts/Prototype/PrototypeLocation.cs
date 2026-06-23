using UnityEngine;

namespace RightOfBlood.Prototype {
    public sealed class PrototypeLocation : MonoBehaviour {
        [SerializeField] private LocationId id;
        [SerializeField] private bool hideWhenInactive = true;
        [SerializeField] private bool useMovementBounds = true;
        [SerializeField] private Vector2 movementBoundsCenter;
        [SerializeField] private Vector2 movementBoundsSize = new Vector2(12f, 8f);
        [SerializeField] private PrototypeSpawnPoint[] spawnPoints;

        public LocationId Id => id;
        public bool HideWhenInactive => hideWhenInactive;
        public bool UseMovementBounds => useMovementBounds;

        public Bounds MovementBounds => new Bounds(movementBoundsCenter,
            new Vector3(movementBoundsSize.x, movementBoundsSize.y, 1f));

        public Transform GetSpawn(string spawnId) {
            if (spawnPoints != null) {
                foreach (var spawnPoint in spawnPoints) {
                    if (spawnPoint != null && spawnPoint.Id == spawnId) {
                        return spawnPoint.transform;
                    }
                }
            }

            return spawnPoints != null && spawnPoints.Length > 0 && spawnPoints[0] != null
                ? spawnPoints[0].transform
                : transform;
        }

        private void OnDrawGizmosSelected() {
            if (!useMovementBounds) return;
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.35f);
            Gizmos.DrawWireCube(movementBoundsCenter, new Vector3(movementBoundsSize.x, movementBoundsSize.y, 1f));
        }
    }
}