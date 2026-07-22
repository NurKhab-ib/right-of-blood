using UnityEngine;

namespace RightOfBlood.Prototype {
    [RequireComponent(typeof(Camera))]
    public sealed class LocationBackgroundController : MonoBehaviour {
        [Header("Muted location backgrounds")]
        [SerializeField] private Color officeColor = new Color(0.10f, 0.20f, 0.42f, 1f);
        [SerializeField] private Color cityColor = new Color(0.45f, 0.47f, 0.50f, 1f);
        [SerializeField] private Color archiveColor = new Color(0.48f, 0.30f, 0.05f, 1f);
        [SerializeField] private Color councilColor = new Color(0.45f, 0.65f, 0.32f, 1f);
        [SerializeField] private Color darkStreetsColor = new Color(0.17f, 0.18f, 0.20f, 1f);

        private Camera sceneCamera;
        private LocationId activeLocation = (LocationId)(-1);

        private void Awake() {
            sceneCamera = GetComponent<Camera>();
            ApplyActiveLocationColor(true);
        }

        private void LateUpdate() {
            ApplyActiveLocationColor(false);
        }

        private void ApplyActiveLocationColor(bool force) {
            var locations = FindObjectsByType<Location>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var location in locations) {
                if (location == null || !location.gameObject.activeInHierarchy) continue;
                if (!force && activeLocation == location.Id) return;
                activeLocation = location.Id;
                sceneCamera.backgroundColor = GetColor(location.Id);
                return;
            }
        }

        private Color GetColor(LocationId location) {
            switch (location) {
                case LocationId.council: return councilColor;
                case LocationId.archive: return archiveColor;
                case LocationId.streets: return darkStreetsColor;
                case LocationId.city: return cityColor;
                default: return officeColor;
            }
        }
    }
}