using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace RightOfBlood.Prototype {
    public sealed class ProgressionUIHandler : MonoBehaviour {
        [SerializeField] private QuestGame game;

        [Header("Panels")]
        [SerializeField] private GameObject skillsPanel;
        [SerializeField] private GameObject reputationPanel;
        [SerializeField] private GameObject flagsPanel;
        [SerializeField] private GameObject questsPanel;
        [SerializeField] private bool hidePanelsOnAwake = true;

        [Header("Text")]
        [SerializeField] private TMP_Text reputationText;
        [SerializeField] private TMP_Text flagsText;
        [SerializeField] private TMP_Text questsText;

        private void Awake() {
            if (!hidePanelsOnAwake) return;
            SetPanel(skillsPanel, false);
            SetPanel(reputationPanel, false);
            SetPanel(flagsPanel, false);
            SetPanel(questsPanel, false);
        }

        private void Update() {
            var keyboard = Keyboard.current;
            if (keyboard == null) return;

            if (keyboard.rKey.wasPressedThisFrame) ToggleReputationPanel();
            if (keyboard.fKey.wasPressedThisFrame) ToggleFlagsPanel();
            if (keyboard.qKey.wasPressedThisFrame) ToggleQuestsPanel();
            if (keyboard.cKey.wasPressedThisFrame) ToggleSkillsPanel();
            if (keyboard.escapeKey.wasPressedThisFrame) CloseAllPanels();

            RefreshVisiblePanelText();
        }

        public void OpenProgressionTree() {
            OpenSkillsPanel();
        }

        public void OpenSkillsPanel() {
            OpenOnly(skillsPanel);
        }

        public void ToggleSkillsPanel() {
            ToggleOnly(skillsPanel);
        }

        public void CloseProgressionTree() {
            SetPanel(skillsPanel, false);
        }

        public void OpenReputationPanel() {
            OpenOnly(reputationPanel);
            RefreshReputationText();
        }

        public void ToggleReputationPanel() {
            ToggleOnly(reputationPanel);
            RefreshReputationText();
        }

        public void CloseReputationPanel() {
            SetPanel(reputationPanel, false);
        }

        public void OpenFlagsPanel() {
            OpenOnly(flagsPanel);
            RefreshFlagsText();
        }

        public void ToggleFlagsPanel() {
            ToggleOnly(flagsPanel);
            RefreshFlagsText();
        }

        public void CloseFlagsPanel() {
            SetPanel(flagsPanel, false);
        }

        public void OpenQuestsPanel() {
            OpenOnly(questsPanel);
            RefreshQuestsText();
        }

        public void ToggleQuestsPanel() {
            ToggleOnly(questsPanel);
            RefreshQuestsText();
        }

        public void CloseQuestsPanel() {
            SetPanel(questsPanel, false);
        }

        public void CloseAllPanels() {
            SetPanel(skillsPanel, false);
            SetPanel(reputationPanel, false);
            SetPanel(flagsPanel, false);
            SetPanel(questsPanel, false);
        }

        public void SimulateArchivistStage() {
            ResolveGame()?.SimulateArchivistStage();
            RefreshVisiblePanelText();
        }

        public void SimulateArchiveManagerStage() {
            ResolveGame()?.SimulateArchiveManagerStage();
            RefreshVisiblePanelText();
        }

        public void SimulateCityManagerStage() {
            ResolveGame()?.SimulateCityManagerStage();
            RefreshVisiblePanelText();
        }

        public void SimulateCouncilParishionerStage() {
            ResolveGame()?.SimulateCouncilParishionerStage();
            RefreshVisiblePanelText();
        }

        public void SimulateCouncilCandidateStage() {
            ResolveGame()?.SimulateCouncilCandidateStage();
            RefreshVisiblePanelText();
        }

        public void SimulateCouncilMemberStage() {
            ResolveGame()?.SimulateCouncilMemberStage();
            RefreshVisiblePanelText();
        }

        public void SimulateRogueNewcomerStage() {
            ResolveGame()?.SimulateRogueNewcomerStage();
            RefreshVisiblePanelText();
        }

        public void SimulateExperiencedRogueStage() {
            ResolveGame()?.SimulateExperiencedRogueStage();
            RefreshVisiblePanelText();
        }

        public void SimulateGuildHeadStage() {
            ResolveGame()?.SimulateGuildHeadStage();
            RefreshVisiblePanelText();
        }

        public void RunScalingCheckQuest() {
            ResolveGame()?.RunScalingCheckQuest();
            RefreshVisiblePanelText();
        }

        public void RunProgressionBehaviorQuest() {
            ResolveGame()?.RunProgressionBehaviorQuest();
            RefreshVisiblePanelText();
        }

        public void RunBuildApproachQuest() {
            ResolveGame()?.RunBuildApproachQuest();
            RefreshVisiblePanelText();
        }

        public void RefreshVisiblePanelText() {
            if (IsPanelOpen(reputationPanel)) RefreshReputationText();
            if (IsPanelOpen(flagsPanel)) RefreshFlagsText();
            if (IsPanelOpen(questsPanel)) RefreshQuestsText();
        }

        private void RefreshReputationText() {
            var questGame = ResolveGame();
            if (reputationText != null && questGame != null) reputationText.text = questGame.GetReputationPanelText();
        }

        private void RefreshFlagsText() {
            var questGame = ResolveGame();
            if (flagsText != null && questGame != null) flagsText.text = questGame.GetDebugFlagsPanelText();
        }

        private void RefreshQuestsText() {
            var questGame = ResolveGame();
            if (questsText != null && questGame != null) questsText.text = questGame.GetQuestPanelText();
        }

        private void OpenOnly(GameObject panel) {
            CloseAllPanels();
            SetPanel(panel, true);
        }

        private void ToggleOnly(GameObject panel) {
            var shouldOpen = !IsPanelOpen(panel);
            CloseAllPanels();
            SetPanel(panel, shouldOpen);
        }

        private static void SetPanel(GameObject panel, bool visible) {
            if (panel != null) panel.SetActive(visible);
        }

        private static bool IsPanelOpen(GameObject panel) {
            return panel != null && panel.activeSelf;
        }

        private QuestGame ResolveGame() {
            if (game == null) game = FindFirstObjectByType<QuestGame>();
            if (game != null) return game;

            var logic = new GameObject("Intro Quest Game Logic");
            game = logic.AddComponent<QuestGame>();
            return game;
        }
    }
}
