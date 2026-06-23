using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    public sealed class PrototypeQuestUi : MonoBehaviour {
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
                stateText.text = $"Совет {state.CouncilReputation:+#;-#;0}   Мафия {state.MafiaReputation:+#;-#;0}   " +
                                 $"Служебное влияние {state.OfficialInfluence}   Угроза {state.ThreatLevel}   Документ: {DocumentStatus(state)}";
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

                if (choiceLabels != null && i < choiceLabels.Length && choiceLabels[i] != null) {
                    choiceLabels[i].text = hasChoice ? $"{i + 1}. {choices[i]}" : string.Empty;
                }
            }
        }

        public void HideDialogue() {
            if (dialoguePanel != null) dialoguePanel.SetActive(false);
        }

        private static string DocumentStatus(IntroQuestState state) {
            if (!state.DocumentFound) return "не найден";
            if (state.Owner == DocumentOwner.council) return "копия у Совета";
            if (state.Owner == DocumentOwner.mafia) return "копия у мафии";
            return "только у игрока";
        }
    }
}

