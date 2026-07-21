using System.Collections.Generic;
using UnityEngine;

namespace RightOfBlood.Prototype {
    /// <summary>Scene validation only. Interactive props are authored in MainScene and never spawned at runtime.</summary>
    public sealed class WorldSceneAuthoring : MonoBehaviour {
        private void Awake() {
            AddPhysicsToPlacedObjects();
            HideLegacyPrototypeButtons();
        }

        private static void HideLegacyPrototypeButtons() {
            var names = new HashSet<string> {
                "Quest3Button", "Quest4Button", "Quest5Button",
                "Magistrate1Button", "Magistrate2Button", "Magistrate3Button",
                "Sage1Button", "Sage2Button", "Sage3Button",
                "Rogue1Button", "Rogue2Button", "Rogue3Button"
            };
            foreach (var button in FindObjectsByType<UnityEngine.UI.Button>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (names.Contains(button.gameObject.name)) button.gameObject.SetActive(false);
            }
        }

        private static void AddPhysicsToPlacedObjects() {
            foreach (var interactable in FindObjectsByType<Interactable>(FindObjectsInactive.Include, FindObjectsSortMode.None)) {
                if (WorldObject.IsNpc(interactable.Kind)) EnsureNpcCollider(interactable.gameObject);
                else RemoveBlockingColliders(interactable.gameObject);
            }
        }

        private static void EnsureNpcCollider(GameObject npc) {
            var collider = npc.GetComponent<BoxCollider2D>();
            if (collider == null) collider = npc.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.offset = new Vector2(0f, -0.1f);
            collider.size = new Vector2(0.55f, 0.7f);
            collider.enabled = true;
        }

        private static void RemoveBlockingColliders(GameObject worldObject) {
            foreach (var collider in worldObject.GetComponents<Collider2D>()) Destroy(collider);
        }
    }
}
