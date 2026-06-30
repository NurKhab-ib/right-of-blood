using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    public sealed class QuestUI : MonoBehaviour {
        [Header("HUD")] [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text promptText;

        [Header("Dialogue")] [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;

        public void RefreshHud(IntroQuestState state, string objective, string prompt) {
            if (objectiveText != null) objectiveText.text = objective;
            if (promptText != null) promptText.text = prompt;
            if (stateText != null) {
                stateText.text = $"Этап {state.Level}   Билд: {BuildStatus(state)}   Реп {CurrentBranchReputation(state)}/{NextReputation(state)}   " +
                                 $"Совет {state.CouncilReputation:+#;-#;0}   Мафия {state.MafiaReputation:+#;-#;0}   " +
                                 $"Служба {state.OfficialInfluence}   Угроза {state.ThreatLevel}   " +
                                 $"Документ {DocumentStatus(state)}   Квест Совета: {CouncilQuestStatus(state)}   Навыки: {SkillStatus(state)}";
            }
        }

        public void ShowDialogue(string speaker, string body, string[] choices, Action<int> onChoice) {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerText != null) speakerText.text = speaker;
            if (bodyText != null) bodyText.text = body;

            var buttonCount = choiceButtons == null ? 0 : choiceButtons.Length;
            for (var i = 0; i < buttonCount; i++) {
                var hasChoice = choices != null && i < choices.Length;
                if (choiceButtons[i] != null) {
                    choiceButtons[i].gameObject.SetActive(hasChoice);
                    choiceButtons[i].onClick.RemoveAllListeners();
                    if (hasChoice) {
                        var choiceIndex = i;
                        choiceButtons[i].onClick.AddListener(() => onChoice?.Invoke(choiceIndex));
                    }
                }

                var choiceLabel = GetChoiceLabel(i);
                if (choiceLabel != null) {
                    choiceLabel.text = hasChoice ? choices[i] : string.Empty;
                }
            }
        }

        public void HideDialogue() {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private TMP_Text GetChoiceLabel(int index) {
            if (choiceLabels != null && index < choiceLabels.Length && choiceLabels[index] != null) {
                return choiceLabels[index];
            }

            if (choiceButtons != null && index < choiceButtons.Length && choiceButtons[index] != null) {
                return choiceButtons[index].GetComponentInChildren<TMP_Text>(true);
            }

            return null;
        }
        private static string NextReputation(IntroQuestState state) {
            return state.Level >= 3 ? "max" : ProgressionModel.GetRequiredReputation(state.Level + 1).ToString();
        }

        private static int CurrentBranchReputation(IntroQuestState state) {
            switch (state.Build) {
                case PlayerBuild.magistrate: return state.OfficialInfluence;
                case PlayerBuild.sage: return state.CouncilReputation;
                case PlayerBuild.rogue: return state.MafiaReputation;
                default: return 0;
            }
        }

        private static string BuildStatus(IntroQuestState state) {
            switch (state.Build) {
                case PlayerBuild.magistrate: return "Магистрат";
                case PlayerBuild.sage: return "Мудрец";
                case PlayerBuild.rogue: return "Разбойник";
                default: return "не выбран";
            }
        }

        private static string SkillStatus(IntroQuestState state) {
            var skills = new System.Collections.Generic.List<string>();
            if (state.ServiceSealUnlocked) skills.Add("Печать");
            if (state.ArchiveProcedureUnlocked) skills.Add("Регламент");
            if (state.BloodEchoUnlocked) skills.Add("Эхо крови");
            if (state.CouncilCipherUnlocked) skills.Add("Шифр");
            if (state.ShadowEntryUnlocked) skills.Add("Тень");
            if (state.StreetDebtUnlocked) skills.Add("Долг");
            if (state.AncientBloodMandateUnlocked) skills.Add("Право крови");
            if (state.PublicLibraryAccessUnlocked) skills.Add("Библиотека");
            if (state.ArchiveDocumentTheftUnlocked) skills.Add("Кража дела");
            return skills.Count == 0 ? "нет" : string.Join(", ", skills);
        }
        private static string DocumentStatus(IntroQuestState state) {
            if (!state.DocumentFound) return "не найден";
            if (state.Owner == DocumentOwner.council) return "копия у Совета";
            if (state.Owner == DocumentOwner.mafia) return "копия у мафии";
            return "только у игрока";
        }

        private static string CouncilQuestStatus(IntroQuestState state) {
            if (state.CouncilQuestStage == CouncilQuestStage.locked) return "закрыт";
            if (state.CouncilQuestStage != CouncilQuestStage.completed) return "активен";

            var solution = state.CouncilSolution == CouncilProblemSolution.law ? "закон" :
                state.CouncilSolution == CouncilProblemSolution.criminal ? "мафия" :
                state.CouncilSolution == CouncilProblemSolution.intrigue ? "интрига" : "решён";

            return state.BloodMagicAdvancedUnlocked ? $"библиотека, {solution}" : solution;
        }
    }
}
