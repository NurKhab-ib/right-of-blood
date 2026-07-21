using UnityEngine;
using UnityEngine.SceneManagement;

namespace RightOfBlood.Prototype {
    public partial class QuestGame {
        private bool IsTerminal => state != null && state.GameEnded;

        private void CompleteEnding(GameOutcome outcome, string title, string summary) {
            state.Outcome = outcome;
            state.GameEnded = true;
            state.EndingSummary = summary;
            SyncWorldStateFlags();
            RefreshUi();
            ShowDialogue(title, summary + "\n\nИстория завершена. Выберите новую историю, чтобы пройти другим путём.",
                new[] { new DialogueChoice("Начать новую историю", RestartStory) });
        }

        private void CompleteDeath(string reason) {
            if (IsTerminal) return;
            CompleteEnding(GameOutcome.death, "Конец: смерть магистрата",
                reason + " Город остаётся без носителя древней крови: Совет прячет знания, Мафия делит улицы, а ритуал так и не получает решения.");
        }

        private void RestartStory() {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void CheckRaidRisk() {
            if (state.ThreatLevel < 8 || state.OfficialInfluence >= 3 || state.CouncilReputation >= 3 || state.MafiaReputation >= 4) return;
            CompleteDeath("Во время облавы у вас нет ни служебной защиты, ни союзника, готового отвести удар.");
        }

        private static GameOutcome OutcomeFor(FinaleChoice choice) {
            switch (choice) {
                case FinaleChoice.summon: return GameOutcome.summoned_creature;
                case FinaleChoice.seal: return GameOutcome.sealed_archive;
                case FinaleChoice.council: return GameOutcome.council_custody;
                case FinaleChoice.mafia: return GameOutcome.mafia_custody;
                case FinaleChoice.reveal: return GameOutcome.public_truth;
                case FinaleChoice.mafia_rule: return GameOutcome.mafia_rule;
                case FinaleChoice.mafia_truce: return GameOutcome.mafia_truce;
                case FinaleChoice.mafia_escape: return GameOutcome.archive_escape;
                default: return GameOutcome.none;
            }
        }
    }
}
