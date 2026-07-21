using System;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace RightOfBlood.Prototype {
    public sealed class QuestUI : MonoBehaviour {
        [Header("HUD")] [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text stateText;
        [SerializeField] private TMP_Text promptText;
        [SerializeField] private TMP_Text cityStateText;

        [Header("Dialogue")] [SerializeField] private GameObject dialoguePanel;
        [SerializeField] private TMP_Text speakerText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Button[] choiceButtons;
        [SerializeField] private TMP_Text[] choiceLabels;

        public void RefreshHud(IntroQuestState state, string objective, string prompt) {
            if (objectiveText != null) objectiveText.text = TextNormalizer.Normalize(objective);
            if (promptText != null) promptText.text = TextNormalizer.Normalize(prompt);
            if (stateText != null) {
                stateText.text = TextNormalizer.Normalize(BuildHudText(state));
            }
            if (cityStateText != null) {
                cityStateText.text = TextNormalizer.Normalize(BuildCityStateText(state));
                cityStateText.color = GetCityStateColor(state.World.Stability);
            }
        }

        public void ShowDialogue(string speaker, string body, string[] choices, Action<int> onChoice) {
            if (dialoguePanel != null) dialoguePanel.SetActive(true);
            if (speakerText != null) speakerText.text = TextNormalizer.Normalize(speaker);
            if (bodyText != null) bodyText.text = TextNormalizer.Normalize(body);

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
                    choiceLabel.text = hasChoice ? TextNormalizer.Normalize(choices[i]) : string.Empty;
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

        private static string BuildHudText(IntroQuestState state) {
            return $"Этап {state.Level}   Билд: {BuildStatus(state)}   Реп {CurrentBranchReputation(state)}/{NextReputation(state)}   " +
                   $"Совет {state.CouncilReputation:+#;-#;0}   Мафия {state.MafiaReputation:+#;-#;0}   " +
                   $"Служба {state.OfficialInfluence}   Угроза {state.ThreatLevel}   " +
                   $"Документ {DocumentStatus(state)}   Квест Совета: {CouncilQuestStatus(state)}   Навыки: {SkillStatus(state)}   " +
                   $"Мир: {WorldSummary(state)}";
        }

        private static string BuildCityStateText(IntroQuestState state) {
            var stability = state.World.Stability;
            return $"{stability} / 100 - {CityStateLabel(stability)}\n" +
                   $"Контроль за: {ControlText(state.CityControl)}\n" +
                   $"Улицы: {StreetText(state.World.Streets)} - {RestrictionText(state.World)}" +
                   (state.GameEnded ? $"\nИтог: {OutcomeText(state.Outcome)}" : state.FinaleChoice == FinaleChoice.none ? string.Empty : $"\nСудьба: {FinaleText(state.FinaleChoice)}");
        }

        private static string FinaleText(FinaleChoice choice) {
            switch (choice) {
                case FinaleChoice.summon: return "призвано Существо";
                case FinaleChoice.seal: return "хранилище запечатано";
                case FinaleChoice.council: return "ритуал у Совета";
                case FinaleChoice.mafia: return "ритуал у Мафии";
                case FinaleChoice.mafia_rule: return "город взят силой";
                case FinaleChoice.mafia_truce: return "мир кварталов";
                case FinaleChoice.mafia_escape: return "архив исчез";
                case FinaleChoice.reveal: return "правда открыта";
                default: return string.Empty;
            }
        }

        private static string OutcomeText(GameOutcome outcome) {
            switch (outcome) {
                case GameOutcome.death: return "магистрат погиб";
                case GameOutcome.summoned_creature: return "Существо призвано";
                case GameOutcome.sealed_archive: return "хранилище запечатано";
                case GameOutcome.council_custody: return "ритуал у Совета";
                case GameOutcome.mafia_custody: return "ритуал у Мафии";
                case GameOutcome.public_truth: return "правда опубликована";
                case GameOutcome.mafia_rule: return "власть улиц";
                case GameOutcome.mafia_truce: return "мир кварталов";
                case GameOutcome.archive_escape: return "архив исчез";
                default: return "не определён";
            }
        }
        private static string CityStateLabel(int stability) {
            if (stability > 70) return "устойчиво";
            if (stability >= 40) return "напряжённо";
            if (stability >= 20) return "кризис";
            return "хаос";
        }

        private static string StreetText(StreetMode streets) {
            switch (streets) {
                case StreetMode.free: return "свободны";
                case StreetMode.patrolled: return "под патрулями";
                case StreetMode.raid: return "под рейдами";
                default: return streets.ToString();
            }
        }

        private static string RestrictionText(WorldState world) {
            if (world.Siege) return "осадное положение";
            if (world.Quarantine) return "карантин";
            return "без ограничений";
        }

        private static Color GetCityStateColor(int stability) {
            if (stability > 70) return new Color(0.45f, 1f, 0.55f);
            if (stability >= 40) return new Color(1f, 0.85f, 0.3f);
            if (stability >= 20) return new Color(1f, 0.55f, 0.2f);
            return new Color(1f, 0.28f, 0.28f);
        }
        private static string WorldSummary(IntroQuestState state) {
            return $"город {WorldMoodText(state.CityMood)} | офис {ControlText(state.OfficeControl)} | " +
                   $"городской контроль {ControlText(state.CityControl)} | архив {ControlText(state.ArchiveControl)} | " +
                   $"Совет {ControlText(state.CouncilControl)} | улицы {ControlText(state.StreetsControl)} | " +
                   $"вход в архив {RouteText(state.ArchiveFrontDoorState)} | чёрный ход {RouteText(state.ArchiveBackDoorState)} | " +
                   $"тайная библиотека {RouteText(state.CouncilSecretLibraryState)} | тёмные улицы {RouteText(state.DarkStreetsRouteState)}";
        }

        private static string WorldMoodText(WorldMoodState state) {
            switch (state) {
                case WorldMoodState.stable: return "стабилен";
                case WorldMoodState.tense: return "напряжён";
                case WorldMoodState.locked_down: return "на карантине";
                case WorldMoodState.crisis: return "в кризисе";
                default: return state.ToString();
            }
        }

        private static string ControlText(WorldControlState state) {
            switch (state) {
                case WorldControlState.magistrate: return "Управление";
                case WorldControlState.council: return "Совет";
                case WorldControlState.mafia: return "Мафия";
                case WorldControlState.contested: return "-";
                default: return state.ToString();
            }
        }

        private static string RouteText(WorldRouteState state) {
            switch (state) {
                case WorldRouteState.closed: return "закрыт";
                case WorldRouteState.open: return "открыт";
                case WorldRouteState.secret: return "тайный";
                default: return state.ToString();
            }
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
                case PlayerBuild.sage: return "Совет";
                case PlayerBuild.rogue: return "Мафия";
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
