using UnityEngine;
using UnityEngine.SceneManagement;

namespace RightOfBlood.Prototype {
    public sealed class IntroQuestBootstrap {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() {
            if (SceneManager.GetActiveScene().name != "SampleScene") {
                return;
            }

            if (Object.FindFirstObjectByType<IntroQuestGame>() != null) {
                return;
            }

            new GameObject("Intro Quest Game Logic").AddComponent<IntroQuestGame>();
        }
    }
}
