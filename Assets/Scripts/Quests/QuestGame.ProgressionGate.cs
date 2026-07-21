namespace RightOfBlood.Prototype {
    public partial class QuestGame {
        private bool CanReachThirdStage() {
            return state.SecondDevelopmentChoiceMade &&
                   state.ScalingCheckQuestStatus == PrototypeQuestStatus.completed &&
                   GetCurrentBranchReputation() >= ProgressionModel.ReputationForThirdLevel;
        }

        private bool CanUnlockAncientBloodMandate() {
            if (state.Build == PlayerBuild.undecided || state.Level < 3 || !state.SecondDevelopmentChoiceMade) return false;
            if (state.ScalingCheckQuestStatus != PrototypeQuestStatus.completed ||
                state.BuildApproachQuestStatus != PrototypeQuestStatus.completed || !state.BloodKnowledgeUnlocked) return false;

            switch (state.Build) {
                case PlayerBuild.magistrate:
                    return state.QuarantineRouteOpen && state.OfficialInfluence >= ProgressionModel.ReputationForThirdLevel;
                case PlayerBuild.sage:
                    return state.CouncilQuestStage == CouncilQuestStage.completed && state.AntidoteDistributed &&
                           state.CouncilReputation >= ProgressionModel.ReputationForThirdLevel;
                case PlayerBuild.rogue:
                    return state.BlackWarehouseDestroyed && state.MafiaReputation >= ProgressionModel.ReputationForThirdLevel;
                default:
                    return false;
            }
        }

        private void OfferSecondDevelopmentChoice() {
            if (state.SecondDevelopmentChoiceMade || GetCurrentBranchReputation() < ProgressionModel.ReputationForSecondLevel) return;
            ShowDialogue("Второе решение развития",
                "Первый серьёзный успех сделал вас заметным. Выберите, как закрепить путь: это открывает второй этап, но делает прежние компромиссы видимыми для соперников.",
                new[] { new DialogueChoice("Закрепить текущий путь", ConfirmSecondDevelopmentChoice),
                        new DialogueChoice("Пока не решать", CloseDialogue) });
        }

        private void ConfirmSecondDevelopmentChoice() {
            state.SecondDevelopmentChoiceMade = true;
            state.SecondDevelopmentChoice = state.Build == PlayerBuild.magistrate ? ProgressionChoice.public_office :
                state.Build == PlayerBuild.sage ? ProgressionChoice.forbidden_knowledge : ProgressionChoice.street_authority;
            RefreshProgressionFromReputation();
            ShowMessage("Путь закреплён", "Открыт второй этап. Для вершины пути нужны репутация 4, проверка закрытого крыла и последствия вашего решения по эпидемии.");
        }

        private string GetMandateRequirementText() {
            switch (state.Build) {
                case PlayerBuild.magistrate: return "Станьте Управляющим городом, пройдите закрытое крыло и завершите эпидемию карантином.";
                case PlayerBuild.sage: return "Станьте Членом Совета, завершите дело Совета, пройдите закрытое крыло и распространите противоядие.";
                case PlayerBuild.rogue: return "Станьте Главой гильдии, пройдите закрытое крыло и уничтожьте чёрный склад.";
                default: return "Выберите и развейте один из путей.";
            }
        }
    }
}
