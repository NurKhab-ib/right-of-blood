using UnityEngine;

namespace RightOfBlood.Prototype {
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PlayerBuildVisual : MonoBehaviour {
        [SerializeField] private Color undecidedColor = new Color(0.48f, 0.76f, 0.88f, 1f);
        [SerializeField] private Color magistrateColor = new Color(0.10f, 0.38f, 1f, 1f);
        [SerializeField] private Color sageColor = new Color(0.14f, 0.85f, 0.25f, 1f);
        [SerializeField] private Color mafiaColor = new Color(1f, 0.12f, 0.16f, 1f);
        [SerializeField, Range(0f, 1f)] private float noviceSaturation = 0.32f;

        private SpriteRenderer spriteRenderer;
        private QuestGame questGame;
        private PlayerBuild lastBuild = (PlayerBuild)(-1);
        private int lastLevel = -1;

        private void Awake() {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        private void LateUpdate() {
            if (questGame == null) questGame = FindFirstObjectByType<QuestGame>();
            var build = questGame == null ? PlayerBuild.undecided : questGame.CurrentBuild;
            var level = questGame == null ? 0 : questGame.CurrentBuildLevel;
            if (build == lastBuild && level == lastLevel) return;

            lastBuild = build;
            lastLevel = level;
            spriteRenderer.color = GetProgressionColor(build, level);
        }

        private Color GetProgressionColor(PlayerBuild build, int level) {
            var baseColor = build == PlayerBuild.magistrate ? magistrateColor :
                build == PlayerBuild.sage ? sageColor :
                build == PlayerBuild.rogue ? mafiaColor : undecidedColor;
            if (build == PlayerBuild.undecided) return baseColor;

            var saturation = Mathf.Lerp(noviceSaturation, 1f, Mathf.InverseLerp(1f, 3f, Mathf.Clamp(level, 1, 3)));
            return Color.Lerp(Color.white, baseColor, saturation);
        }
    }
}