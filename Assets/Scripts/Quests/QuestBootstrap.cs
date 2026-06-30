using UnityEngine;
using UnityEngine.SceneManagement;

namespace RightOfBlood.Prototype {
    public sealed class QuestBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            var sceneName = SceneManager.GetActiveScene().name;
            if (sceneName != "MainScene" && sceneName != "SampleScene") {
                return;
            }

            if (Object.FindFirstObjectByType<QuestGame>() != null) {
                return;
            }

            new GameObject("Intro Quest Game Logic").AddComponent<QuestGame>();
        }
    }
}
